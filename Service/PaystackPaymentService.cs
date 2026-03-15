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

        // Ensure the base URL is set and normalized.
        var baseUrl = (_settings.BaseUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = "https://api.paystack.co";
        else
            baseUrl = baseUrl.TrimEnd('/');

        _http.BaseAddress = new Uri(baseUrl);
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
            throw new InvalidOperationException("Customer email is required for Paystack payment initialization.");

        // Paystack expects amount in kobo (NGN smallest unit) or cents.
        // Multiply by 100 the same way Stripe does — works for NGN, GHS, ZAR, USD.
        var amountInKobo = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);

        // Unique reference for this transaction — use orderId for easy reconciliation
        var reference = $"order_{orderId:N}";

        var payload = new
        {
            email = customerEmail,
            amount = amountInKobo,
            currency = currency.ToUpper(),   // Paystack uses uppercase: NGN, USD, GHS
            reference = reference,
            metadata = new { order_id = orderId.ToString() }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("/transaction/initialize", content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Paystack initialization failed ({(int)response.StatusCode}): {body}");

        var result = JsonSerializer.Deserialize<PaystackInitializeResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Empty response from Paystack.");

        if (!result.Status)
            throw new InvalidOperationException(
                $"Paystack error: {result.Message}");

        return new PaymentResult(
            PaymentData: result.Data.AuthorizationUrl,  // redirect user here
            PaymentReference: result.Data.Reference          // store on Order
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
            // Paystack signs with HMAC-SHA512 using the secret key as the key.
            // The header sent is "x-paystack-signature".
            var keyBytes = Encoding.UTF8.GetBytes(_settings.WebhookSecret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            var hash = HMACSHA512.HashData(keyBytes, payloadBytes);
            var expectedHash = Convert.ToHexString(hash).ToLower();

            if (!expectedHash.Equals(signature, StringComparison.OrdinalIgnoreCase))
                return false;

            // Parse the event to extract type and reference
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            eventType = root.TryGetProperty("event", out var evt)
                ? evt.GetString() ?? string.Empty
                : string.Empty;

            // Paystack reference is nested at data.reference
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

    // ── Response models for Paystack API deserialization ─────────────────────

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
}