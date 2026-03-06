using System;

namespace Entities.ConfigurationModels;

public class PaymentSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}
