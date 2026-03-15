namespace Service.Contracts;

/// <summary>
/// Provider-agnostic payment abstraction.
/// Payment providers return a URL/token that the frontend uses to complete payment.
/// Both are surfaced through PaymentResult so OrderService stays unchanged
/// regardless of which provider is active.
/// </summary>
public interface IPaymentService
{
    /// <summary>The provider name stored on Order.PaymentProvider.</summary>
    string ProviderName { get; }

    /// <summary>
    /// Initiates a payment and returns a result containing the data the
    /// frontend needs (authorization_url for Paystack / payment link for Flutterwave)
    /// plus the internal reference to store on the Order.
    /// </summary>
    Task<PaymentResult> CreatePaymentIntentAsync(
        decimal amount, string currency, Guid orderId, string? customerEmail = null);

    /// <summary>
    /// Validates an incoming webhook payload and extracts the event type
    /// and the payment reference that links back to an Order.
    /// </summary>
    bool ValidateWebhookSignature(
        string payload,
        string signature,
        out string eventType,
        out string paymentReference);

    /// <summary>
    /// Actively queries the provider's API to confirm whether a transaction
    /// completed successfully. Called after the user returns from the payment
    /// page so we can mark the order as Paid immediately — without waiting for
    /// a webhook.
    /// </summary>
    Task<bool> VerifyTransactionAsync(string reference);
}

/// <summary>
/// Unified result returned by both payment providers after initiating a payment.
/// </summary>
public record PaymentResult(
    /// <summary>
    /// The data the frontend needs to complete payment:
    ///   Paystack  → authorization_url (redirect the user here)
    ///   Flutterwave → payment link   (redirect the user here)
    /// </summary>
    string PaymentData,

    /// <summary>
    /// The reference stored on the Order to reconcile the webhook/verify call later.
    ///   Paystack    → reference (e.g. "order_abc123")
    ///   Flutterwave → tx_ref   (e.g. "order_abc123")
    /// </summary>
    string PaymentReference
);
