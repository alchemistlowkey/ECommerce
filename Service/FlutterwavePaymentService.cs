using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Entities.ConfigurationModels;
using Microsoft.Extensions.Options;
using Service.Contracts;

namespace Service;

public class FlutterwavePaymentService : IPaymentService
{
    private readonly FlutterwaveSettings _settings;
    private readonly HttpClient _http;

    public string ProviderName => "Flutterwave";

    public FlutterwavePaymentService(
        IOptions<FlutterwaveSettings> settings,
        IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _http = httpClientFactory.CreateClient("Flutterwave");

        _http.BaseAddress = new Uri(_settings.BaseUrl);
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.SecretKey);
        _http.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<PaymentResult> CreatePaymentIntentAsync(
        decimal amount, string currency, Guid orderId, string? customerEmail = null)
    {
        if (amount <= 0)
            throw new InvalidOperationException(
                $"Payment amount must be greater than zero. Got: {amount:C}.");

        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new InvalidOperationException("Customer email is required for Flutterwave payment initialization.");

        // Flutterwave accepts the FULL amount in the currency's major unit (e.g. 5000.00 NGN),
        // unlike Paystack which wants kobo. Do NOT multiply by 100.
        var txRef = $"order_{orderId:N}";

        var payload = new
        {
            tx_ref = txRef,
            amount = Math.Round(amount, 2),
            currency = currency.ToUpper(),   // NGN, USD, GHS, etc.
            redirect_url = _settings.RedirectUrl,
            customer = new { email = customerEmail },
            meta = new { order_id = orderId.ToString() }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Correct Flutterwave endpoint: POST /v3/payments  (NOT /transaction/initialize)
        var response = await _http.PostAsync("/payments", content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Flutterwave initialization failed ({(int)response.StatusCode}): {body}");

        var result = JsonSerializer.Deserialize<FlutterwaveInitializeResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Empty response from Flutterwave.");

        if (result.Status != "success")
            throw new InvalidOperationException($"Flutterwave error: {result.Message}");

        return new PaymentResult(
            PaymentData: result.Data.Link,   // redirect the user to this URL
            PaymentReference: txRef          // store tx_ref on Order for later verification
        );
    }

    public bool ValidateWebhookSignature(
        string payload,
        string signature,
        out string eventType,
        out string paymentReference)
    {
        eventType = string.Empty;
        paymentReference = string.Empty;

        try
        {
            // Flutterwave does NOT use HMAC. It sends your WebhookSecret as a plain string
            // in the "verif-hash" header. Simply compare it directly.
            if (!signature.Equals(_settings.WebhookSecret, StringComparison.Ordinal))
                return false;

            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            eventType = root.TryGetProperty("event", out var evt)
                ? evt.GetString() ?? string.Empty
                : string.Empty;

            // Flutterwave sends our tx_ref in data.tx_ref
            paymentReference = root
                .TryGetProperty("data", out var data) &&
                data.TryGetProperty("tx_ref", out var refProp)
                    ? refProp.GetString() ?? string.Empty
                    : string.Empty;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Actively verifies a transaction with the Flutterwave API using the tx_ref.
    /// Called after the user is redirected back to confirm the payment succeeded.
    /// </summary>
    public async Task<bool> VerifyTransactionAsync(string txRef)
    {
        if (string.IsNullOrWhiteSpace(txRef)) return false;

        // GET /v3/transactions/verify_by_reference?tx_ref={txRef}
        var response = await _http.GetAsync($"/transactions/verify_by_reference?tx_ref={Uri.EscapeDataString(txRef)}");
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) return false;

        var result = JsonSerializer.Deserialize<FlutterwaveVerifyResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // status == "success" at root level AND data.status == "successful"
        return result?.Status == "success" &&
               string.Equals(result.Data?.Status, "successful", StringComparison.OrdinalIgnoreCase);
    }

    // ── Private response models ──────────────────────────────────────────────

    private record FlutterwaveInitializeResponse(
        string Status,
        string Message,
        FlutterwaveInitializeData Data
    );

    private record FlutterwaveInitializeData(
        [property: JsonPropertyName("link")] string Link
    );

    private record FlutterwaveVerifyResponse(
        string Status,
        string Message,
        FlutterwaveVerifyData? Data
    );

    private record FlutterwaveVerifyData(
        [property: JsonPropertyName("status")] string Status  // "successful" | "failed" | "pending"
    );
}
