using Entities.ConfigurationModels;
using Microsoft.Extensions.Options;
using Service.Contracts;
using Stripe;

namespace Service;

public class StripePaymentService : IPaymentService
{
    private readonly StripeSettings _settings;

    public string ProviderName => "Stripe";

    public StripePaymentService(IOptions<StripeSettings> settings)
    {
        _settings = settings.Value;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<PaymentResult> CreatePaymentIntentAsync(
        decimal amount, string currency, Guid orderId)
    {
        const decimal stripeMaxAmount = 999_999.99m;

        if (amount <= 0)
            throw new InvalidOperationException(
                $"Payment amount must be greater than zero. Got: {amount:C}.");

        if (amount > stripeMaxAmount)
            throw new InvalidOperationException(
                $"Order total {amount:C} exceeds the maximum chargeable amount of {stripeMaxAmount:C}. " +
                $"Please reduce the order quantity or split into multiple orders.");

        var amountInCents = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInCents,
            Currency = currency.ToLower(),
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            },
            Metadata = new Dictionary<string, string>
            {
                { "orderId", orderId.ToString() }
            }
        };

        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options);

        // clientSecret format: pi_xxx_secret_yyy
        // The PaymentIntent ID is the portion before "_secret_"
        var paymentIntentId = intent.ClientSecret.Split("_secret_")[0];

        return new PaymentResult(
            PaymentData: intent.ClientSecret,
            PaymentReference: paymentIntentId
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
            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                _settings.WebhookSecret
            );

            eventType = stripeEvent.Type;

            if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
                paymentReference = paymentIntent.Id;

            return true;
        }
        catch (StripeException)
        {
            return false;
        }
    }
}