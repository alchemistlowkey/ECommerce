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
            .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<PaymentResult> CreatePaymentIntentAsync(
        decimal amount, string currency, Guid orderId, string? customerEmail = null)
    {
        if (amount <= 0)
            throw new InvalidOperationException(
                $"Payment amount must be greater than zero. Got: {amount:C}.");

        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new InvalidOperationException("Customer email is required for Flutterwave.");

        // Flutterwave expects the FULL amount (not kobo) — it takes the amount as-is in the currency's major unit
        var txRef = $"order_{orderId:N}";

        var payload = new
        {
            tx_ref = txRef,
            amount = Math.Round(amount, 2),  // Full amount e.g. 5000.00 NGN
            currency = currency.ToUpper(),
            redirect_url = _settings.RedirectUrl,
            customer = new { email = customerEmail },
            meta = new { order_id = orderId.ToString() }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Correct Flutterwave endpoint: POST /payments
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
            PaymentData: result.Data.Link,   // redirect URL
            PaymentReference: txRef          // store tx_ref on Order for webhook matching
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
            // Flutterwave sends verif-hash as a PLAIN SECRET STRING (not HMAC)
            // Just compare the header value directly to your WebhookSecret
            if (!signature.Equals(_settings.WebhookSecret, StringComparison.Ordinal))
                return false;

            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            eventType = root.TryGetProperty("event", out var evt)
                ? evt.GetString() ?? string.Empty
                : string.Empty;

            // Flutterwave sends tx_ref (our reference) in data.tx_ref
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

    // ── Verify a transaction directly with the Flutterwave API ───────────────
    public async Task<bool> VerifyTransactionAsync(string txRef)
    {
        // GET /transactions/verify_by_reference?tx_ref={txRef}
        var response = await _http.GetAsync($"/transactions/verify_by_reference?tx_ref={txRef}");
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) return false;

        var result = JsonSerializer.Deserialize<FlutterwaveVerifyResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result?.Status == "success" && result.Data?.Status == "successful";
    }

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
        string Status  // "successful", "failed", "pending"
    );
}