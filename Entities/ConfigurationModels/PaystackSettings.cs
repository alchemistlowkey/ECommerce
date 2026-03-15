namespace Entities.ConfigurationModels;

public class PaystackSettings : PaymentSettings
{
    public string BaseUrl { get; set; } = "https://api.paystack.co";
    public string RedirectUrl { get; set; } = "https://csharp-ecommerce-frontend.vercel.app/payment-complete";
}