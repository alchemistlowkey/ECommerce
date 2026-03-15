using Shared.DataTransferObjects.Order;

namespace Service.Contracts;

public interface IOrderService
{
    Task<CheckoutResponseDto> CheckoutAsync(string userId, string paymentProvider);
    Task<IEnumerable<OrderResponseDto>> GetUserOrdersAsync(string userId);
    Task<OrderResponseDto> GetOrderAsync(string userId, Guid orderId);
    Task HandleFlutterwaveWebhookAsync(string payload, string flutterwaveSignature);
    Task HandlePaystackWebhookAsync(string payload, string paystackSignature);

    /// <summary>
    /// Called by the frontend after the user is redirected back from the payment
    /// page. Actively verifies the transaction with the provider and marks the
    /// order as Paid (or Cancelled) so the user sees the correct status immediately,
    /// without having to wait for the webhook to arrive.
    /// </summary>
    Task<OrderResponseDto> VerifyPaymentAsync(string userId, Guid orderId);
}
