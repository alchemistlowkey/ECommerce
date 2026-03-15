using System;
using Shared.DataTransferObjects.Order;

namespace Service.Contracts;

public interface IOrderService
{
    Task<CheckoutResponseDto> CheckoutAsync(string userId, string paymentProvider);
    Task<IEnumerable<OrderResponseDto>> GetUserOrdersAsync(string userId);
    Task<OrderResponseDto> GetOrderAsync(string userId, Guid orderId);
    Task HandleFlutterwaveWebhookAsync(string payload, string flutterwaveSignature);
    Task HandlePaystackWebhookAsync(string payload, string paystackSignature);
}
