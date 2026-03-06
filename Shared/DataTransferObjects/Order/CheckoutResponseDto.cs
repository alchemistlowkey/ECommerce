namespace Shared.DataTransferObjects.Order;

public record CheckoutResponseDto
{
    public Guid OrderId { get; init; }
    public string PaymentData { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string PaymentProvider { get; init; } = string.Empty;
}
