using AutoMapper;
using Contracts;
using Entities.Models;
using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using Shared.DataTransferObjects.Order;

namespace Service;

public class OrderService : IOrderService
{
    private readonly IRepositoryManager _repository;
    private readonly IMapper _mapper;
    private readonly IServiceProvider _services;

    public OrderService(
        IRepositoryManager repository,
        IMapper mapper,
        IServiceProvider services)
    {
        _repository = repository;
        _mapper = mapper;
        _services = services;
    }

    // Resolve the correct payment provider at runtime based on the user's choice.
    // Both are registered as concrete scoped types in ServiceExtensions.
    private IPaymentService GetPaymentService(string? provider) =>
        (provider ?? string.Empty).Equals("Flutterwave", StringComparison.OrdinalIgnoreCase)
            ? _services.GetRequiredService<FlutterwavePaymentService>()
            : _services.GetRequiredService<PaystackPaymentService>();

    public async Task<CheckoutResponseDto> CheckoutAsync(string userId, string paymentProvider)
    {
        var cart = await _repository.Cart
            .GetCartByUserIdAsync(userId, trackChanges: false)
            ?? throw new InvalidOperationException("Your cart is empty.");

        if (!cart.Items.Any())
            throw new InvalidOperationException("Your cart is empty.");

        var orderItems = new List<OrderItem>();
        decimal total = 0;

        foreach (var cartItem in cart.Items)
        {
            var product = await _repository.Product
                .GetProductAsync(cartItem.ProductId, trackChanges: true)
                ?? throw new KeyNotFoundException($"Product '{cartItem.ProductId}' was not found.");

            if (!product.IsActive)
                throw new InvalidOperationException($"Product '{product.Name}' is no longer available.");

            if (product.Stock < cartItem.Quantity)
                throw new InvalidOperationException(
                    $"Insufficient stock for '{product.Name}'. " +
                    $"Requested: {cartItem.Quantity}, Available: {product.Stock}.");

            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = cartItem.Quantity,
                UnitPrice = product.Price
            });

            product.Stock -= cartItem.Quantity;
            total += product.Price * cartItem.Quantity;
        }

        var payment = GetPaymentService(paymentProvider);

        var user = await _repository.User.GetUserByIdAsync(userId, trackChanges: false)
            ?? throw new KeyNotFoundException("User not found.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Items = orderItems,
            TotalAmount = total,
            Status = OrderStatus.PaymentProcessing,
            PaymentProvider = payment.ProviderName,
            CreatedAt = DateTime.UtcNow
        };

        _repository.Order.CreateOrder(order);

        var result = await payment.CreatePaymentIntentAsync(
            total,
            "NGN",
            order.Id,
            user.Email);

        // Store the provider transaction reference so we can match webhook callbacks
        // and direct verification calls later.
        order.PaystackReference = result.PaymentReference;

        await _repository.SaveAsync();
        await _repository.Cart.DeleteCartAsync(cart.Id);

        return new CheckoutResponseDto
        {
            OrderId = order.Id,
            PaymentData = result.PaymentData,
            TotalAmount = total,
            PaymentProvider = payment.ProviderName
        };
    }

    public async Task<IEnumerable<OrderResponseDto>> GetUserOrdersAsync(string userId)
    {
        var orders = await _repository.Order
            .GetOrdersByUserIdAsync(userId, trackChanges: false);
        return _mapper.Map<IEnumerable<OrderResponseDto>>(orders);
    }

    public async Task<OrderResponseDto> GetOrderAsync(string userId, Guid orderId)
    {
        var order = await _repository.Order
            .GetOrderAsync(orderId, trackChanges: false)
            ?? throw new KeyNotFoundException($"Order with id '{orderId}' was not found.");

        if (order.UserId != userId)
            throw new UnauthorizedAccessException("You are not authorized to view this order.");

        return _mapper.Map<OrderResponseDto>(order);
    }

    /// <summary>
    /// Actively verifies a transaction with the payment provider and updates the
    /// order status to Paid (or Cancelled). Called from the frontend after the
    /// user is redirected back from the payment page — this ensures the status
    /// is updated immediately without relying solely on the webhook.
    /// </summary>
    public async Task<OrderResponseDto> VerifyPaymentAsync(string userId, Guid orderId)
    {
        var order = await _repository.Order.GetOrderAsync(orderId, trackChanges: true)
            ?? throw new KeyNotFoundException($"Order '{orderId}' not found.");

        if (order.UserId != userId)
            throw new UnauthorizedAccessException("You are not authorized to access this order.");

        // If the order is already in a terminal state, just return it as-is.
        if (order.Status == OrderStatus.Paid ||
            order.Status == OrderStatus.Shipped ||
            order.Status == OrderStatus.Delivered ||
            order.Status == OrderStatus.Cancelled)
        {
            return _mapper.Map<OrderResponseDto>(order);
        }

        // Actively call the provider to verify the transaction.
        if (!string.IsNullOrEmpty(order.PaystackReference))
        {
            var payment = GetPaymentService(order.PaymentProvider);
            var verified = await payment.VerifyTransactionAsync(order.PaystackReference);

            order.Status = verified ? OrderStatus.Paid : order.Status;
            await _repository.SaveAsync();
        }

        return _mapper.Map<OrderResponseDto>(order);
    }

    public async Task HandleFlutterwaveWebhookAsync(string payload, string flutterwaveSignature)
    {
        var flutterwave = _services.GetRequiredService<FlutterwavePaymentService>();
        var isValid = flutterwave.ValidateWebhookSignature(
            payload, flutterwaveSignature, out var eventType, out var paymentReference);

        if (!isValid)
            throw new UnauthorizedAccessException("Invalid Flutterwave webhook signature.");

        var order = await _repository.Order
            .GetOrderByPaystackReferenceAsync(paymentReference, trackChanges: true);

        if (order is null) return;

        // Flutterwave webhook event names:
        // "charge.completed" — payment succeeded
        // "charge.failed"    — payment failed
        order.Status = eventType switch
        {
            "charge.completed" => OrderStatus.Paid,
            "charge.failed" => OrderStatus.Cancelled,
            _ => order.Status
        };

        await _repository.SaveAsync();
    }

    public async Task HandlePaystackWebhookAsync(string payload, string paystackSignature)
    {
        var paystack = _services.GetRequiredService<PaystackPaymentService>();
        var isValid = paystack.ValidateWebhookSignature(
            payload, paystackSignature, out var eventType, out var paymentReference);

        if (!isValid)
            throw new UnauthorizedAccessException("Invalid Paystack webhook signature.");

        var order = await _repository.Order
            .GetOrderByPaystackReferenceAsync(paymentReference, trackChanges: true);

        if (order is null) return;

        // Paystack webhook event names:
        // "charge.success"      — payment succeeded
        // "charge.failed"       — payment failed
        // "transfer.reversed"   — refund/reversal
        order.Status = eventType switch
        {
            "charge.success" => OrderStatus.Paid,
            "charge.failed" => OrderStatus.Cancelled,
            "transfer.reversed" => OrderStatus.Cancelled,
            _ => order.Status
        };

        await _repository.SaveAsync();
    }
}
