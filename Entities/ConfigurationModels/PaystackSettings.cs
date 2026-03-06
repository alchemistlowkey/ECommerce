namespace Entities.ConfigurationModels;

public class PaystackSettings : PaymentSettings
{
    public string BaseUrl { get; set; } = "https://api.paystack.co";
}