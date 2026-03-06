using AutoMapper;
using Contracts;
using Entities.Models;
using Service.Contracts;
using Shared.DataTransferObjects.Order;

namespace Service;

public class OrderService : IOrderService
{
    private readonly IRepositoryManager _repository;
    private readonly IMapper _mapper;
    private readonly IPaymentService _payment;

    public OrderService(
        IRepositoryManager repository,
        IMapper mapper,
        IPaymentService payment)
    {
        _repository = repository;
        _mapper = mapper;
        _payment = payment;
    }

    public async Task<CheckoutResponseDto> CheckoutAsync(string userId)
    {
        var cart = await _repository.Cart
            .GetCartByUserIdAsync(userId, trackChanges: true)
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
                throw new InvalidOperationException(
                    $"Product '{product.Name}' is no longer available.");

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

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Items = orderItems,
            TotalAmount = total,
            Status = OrderStatus.PaymentProcessing,
            PaymentProvider = _payment.ProviderName,
            CreatedAt = DateTime.UtcNow
        };

        _repository.Order.CreateOrder(order);

        // PaymentResult is provider-agnostic:
        //   Stripe  → PaymentData = clientSecret,       PaymentReference = pi_xxx
        //   Paystack → PaymentData = authorization_url, PaymentReference = trx reference
        var result = await _payment.CreatePaymentIntentAsync(total, "usd", order.Id);

        // Store the provider-specific reference for webhook reconciliation
        if (_payment.ProviderName == "Stripe")
            order.StripePaymentIntentId = result.PaymentReference;
        else
            order.PaystackReference = result.PaymentReference;

        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveAsync();

        return new CheckoutResponseDto
        {
            OrderId = order.Id,
            PaymentData = result.PaymentData,
            TotalAmount = total,
            PaymentProvider = _payment.ProviderName
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
            throw new UnauthorizedAccessException(
                "You are not authorized to view this order.");

        return _mapper.Map<OrderResponseDto>(order);
    }

    public async Task HandleStripeWebhookAsync(string payload, string stripeSignature)
    {
        var isValid = _payment.ValidateWebhookSignature(
            payload, stripeSignature,
            out var eventType, out var paymentReference);

        if (!isValid)
            throw new UnauthorizedAccessException("Invalid Stripe webhook signature.");

        var order = await _repository.Order
            .GetOrderByPaymentIntentAsync(paymentReference, trackChanges: true);

        if (order is null) return;

        order.Status = eventType switch
        {
            "payment_intent.succeeded" => OrderStatus.Paid,
            "payment_intent.payment_failed" => OrderStatus.Cancelled,
            "payment_intent.canceled" => OrderStatus.Cancelled,
            _ => order.Status
        };

        await _repository.SaveAsync();
    }

    public async Task HandlePaystackWebhookAsync(string payload, string paystackSignature)
    {
        var isValid = _payment.ValidateWebhookSignature(
            payload, paystackSignature,
            out var eventType, out var paymentReference);

        if (!isValid)
            throw new UnauthorizedAccessException("Invalid Paystack webhook signature.");

        var order = await _repository.Order
            .GetOrderByPaystackReferenceAsync(paymentReference, trackChanges: true);

        if (order is null) return;

        // Paystack event types: https://paystack.com/docs/payments/webhooks/#events
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