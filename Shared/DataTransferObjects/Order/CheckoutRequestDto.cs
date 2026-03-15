namespace Shared.DataTransferObjects.Order;

public record CheckoutRequestDto
{
    public string PaymentProvider { get; init; } = "Flutterwave";
}
