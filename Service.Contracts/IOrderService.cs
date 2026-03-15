using Shared.DataTransferObjects.Order;

namespace Service.Contracts;

public interface IOrderService
{
    Task<CheckoutResponseDto> CheckoutAsync(string userId, string paymentProvider);
    Task<IEnumerable<OrderResponseDto>> GetUserOrdersAsync(string userId);
    Task<OrderResponseDto> GetOrderAsync(string userId, Guid orderId);
    Task HandleFlutterwaveWebhookAsync(string payload, string signature);
    Task HandlePaystackWebhookAsync(string payload, string signature);
    // New: called from frontend after payment redirect
    Task<OrderResponseDto> VerifyPaymentAsync(string userId, Guid orderId);
}