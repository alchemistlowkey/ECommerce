using System;
using Shared.DataTransferObjects.Order;

namespace Service.Contracts;

public interface IOrderService
{
    Task<CheckoutResponseDto> CheckoutAsync(string userId, string paymentProvider);
    Task<IEnumerable<OrderResponseDto>> GetUserOrdersAsync(string userId);
    Task<OrderResponseDto> GetOrderAsync(string userId, Guid orderId);
    Task HandleStripeWebhookAsync(string payload, string stripeSignature);
    Task HandlePaystackWebhookAsync(string payload, string paystackSignature);
}
