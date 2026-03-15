namespace Service.Contracts;

/// <summary>
/// Provider-agnostic payment abstraction.
/// Payment providers return a URL/token that the frontend uses to complete payment.
/// Both are surfaced through PaymentResult so OrderService stays unchanged
/// regardless of which provider is active.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Initiates a payment and returns a result containing the data the
    /// frontend needs (authorization_url for Paystack/Flutterwave)
    /// plus the internal reference to store on the Order.
    /// </summary>
    Task<PaymentResult> CreatePaymentIntentAsync(decimal amount, string currency, Guid orderId, string? customerEmail = null);

    /// <summary>
    /// Validates an incoming webhook payload and extracts the event type
    /// and the payment reference that links back to an Order.
    /// </summary>
    bool ValidateWebhookSignature(
        string payload,
        string signature,
        out string eventType,
        out string paymentReference);

    /// <summary>The provider name stored on Order.PaymentProvider.</summary>
    string ProviderName { get; }
}

/// <summary>
/// Unified result returned by both payment providers after initiating a payment.
/// </summary>
public record PaymentResult(
    /// <summary>
    /// The data the frontend needs to complete payment:
    ///   Paystack/Flutterwave → authorization_url (redirect the user here)
    /// </summary>
    string PaymentData,

    /// <summary>
    /// The reference stored on the Order to reconcile the webhook later:
    ///   Provider transaction reference (e.g., trx_xxx).
    /// </summary>
    string PaymentReference
);