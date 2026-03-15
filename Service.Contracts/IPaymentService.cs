namespace Service.Contracts;

public record PaymentResult(string PaymentData, string PaymentReference);

public interface IPaymentService
{
    string ProviderName { get; }

    Task<PaymentResult> CreatePaymentIntentAsync(
        decimal amount, string currency, Guid orderId, string? customerEmail = null);

    bool ValidateWebhookSignature(
        string payload,
        string signature,
        out string eventType,
        out string paymentReference);

    // Called by the frontend after redirect to actively verify payment
    Task<bool> VerifyTransactionAsync(string reference);
}