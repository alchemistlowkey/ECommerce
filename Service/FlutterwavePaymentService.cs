using System.Net.Http.Headers;
using System.Security.Cryptography;
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

        // Flutterwave expects all API calls to be made against the /v3 endpoint.
        // If the configuration is missing the suffix, append it so developers
        // don’t accidentally point to a non-existent endpoint and get 404s.
        var baseUrl = (_settings.BaseUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = "https://api.flutterwave.com/v3";
        else if (!baseUrl.EndsWith("/v3", StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl.TrimEnd('/') + "/v3";

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
            throw new InvalidOperationException("Customer email is required for Flutterwave payment initialization.");

        const string ngn = "NGN";

        // Flutterwave expects amount (in main currency units) and a unique tx_ref
        var txRef = $"order_{orderId:N}";

        var payload = new
        {
            tx_ref = txRef,
            amount = Math.Round(amount, 2),
            currency = ngn,
            redirect_url = _settings.RedirectUrl,
            customer = new
            {
                email = customerEmail
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("/payments", content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Flutterwave initialization failed ({(int)response.StatusCode}): {body}");

        var result = JsonSerializer.Deserialize<FlutterwaveInitializeResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Empty response from Flutterwave.");

        if (!result.Status)
            throw new InvalidOperationException(
                $"Flutterwave error: {result.Message}");

        return new PaymentResult(
            PaymentData: result.Data.Link,
            PaymentReference: txRef
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
            // Flutterwave uses the "verif-hash" header containing the SHA256 HMAC of the payload
            var keyBytes = Encoding.UTF8.GetBytes(_settings.WebhookSecret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
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
                data.TryGetProperty("tx_ref", out var txRef)
                    ? txRef.GetString() ?? string.Empty
                    : string.Empty;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private record FlutterwaveInitializeResponse(
        bool Status,
        string Message,
        FlutterwaveInitializeData Data
    );

    private record FlutterwaveInitializeData(
        [property: JsonPropertyName("link")] string Link
    );
}
