namespace Entities.ConfigurationModels;

public class FlutterwaveSettings : PaymentSettings
{
    public string BaseUrl { get; set; } = "https://api.flutterwave.com/v3/";
    public string RedirectUrl { get; set; } = "https://csharp-ecommerce-frontend.vercel.app/payment-complete";
}
