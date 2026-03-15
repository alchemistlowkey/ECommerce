using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Entities.ConfigurationModels;
using Microsoft.Extensions.Options;
using Service.Contracts;

namespace Service;

public class PaystackPaymentService : IPaymentService
{
    private readonly PaystackSettings _settings;
    private readonly HttpClient _http;

    public string ProviderName => "Paystack";

    public PaystackPaymentService(
        IOptions<PaystackSettings> settings,
        IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _http = httpClientFactory.CreateClient("Paystack");

        _http.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + '/');
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
            throw new InvalidOperationException(
                "Customer email is required for Paystack payment initialization.");

        // Paystack expects amount in kobo (smallest NGN unit) — multiply by 100.
        var amountInKobo = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
        var reference = $"order_{orderId:N}";

        var payload = new
        {
            email = customerEmail,
            amount = amountInKobo,
            currency = currency.ToUpper(),
            reference = reference,
            // Explicitly set callback_url so Paystack always redirects here
            // after payment, regardless of what's configured in the dashboard.
            callback_url = _settings.RedirectUrl,
            metadata = new { order_id = orderId.ToString() }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("transaction/initialize", content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Paystack initialization failed ({(int)response.StatusCode}): {body}");

        var result = JsonSerializer.Deserialize<PaystackInitializeResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Empty response from Paystack.");

        if (!result.Status)
            throw new InvalidOperationException($"Paystack error: {result.Message}");

        return new PaymentResult(
            PaymentData: result.Data.AuthorizationUrl,
            PaymentReference: result.Data.Reference
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
            // Paystack signs with HMAC-SHA512 using the secret key as the HMAC key.
            var keyBytes = Encoding.UTF8.GetBytes(_settings.WebhookSecret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            var hash = HMACSHA512.HashData(keyBytes, payloadBytes);
            var expectedHash = Convert.ToHexString(hash).ToLower();

            if (!expectedHash.Equals(signature, StringComparison.OrdinalIgnoreCase))
                return false;

            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            eventType = root.TryGetProperty("event", out var evt)
                ? evt.GetString() ?? string.Empty
                : string.Empty;

            paymentReference = root
                .TryGetProperty("data", out var data) &&
                data.TryGetProperty("reference", out var refProp)
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
    /// Actively verifies a Paystack transaction by reference.
    /// Called by VerifyPaymentAsync after the user returns from the payment page.
    /// </summary>
    public async Task<bool> VerifyTransactionAsync(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return false;

        // GET /transaction/verify/{reference}
        var response = await _http.GetAsync(
            $"transaction/verify/{Uri.EscapeDataString(reference)}");
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) return false;

        var result = JsonSerializer.Deserialize<PaystackVerifyResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Root status:true AND data.status:"success" both must be present
        return result?.Status == true &&
               string.Equals(result.Data?.Status, "success", StringComparison.OrdinalIgnoreCase);
    }

    // ── Private response models ──────────────────────────────────────────────

    private record PaystackInitializeResponse(
        bool Status,
        string Message,
        PaystackInitializeData Data
    );

    private record PaystackInitializeData(
        [property: JsonPropertyName("authorization_url")] string AuthorizationUrl,
        [property: JsonPropertyName("access_code")] string AccessCode,
        [property: JsonPropertyName("reference")] string Reference
    );

    private record PaystackVerifyResponse(
        bool Status,
        string Message,
        PaystackVerifyData? Data
    );

    private record PaystackVerifyData(
        [property: JsonPropertyName("status")] string Status  // "success" | "failed" | "abandoned"
    );
}
