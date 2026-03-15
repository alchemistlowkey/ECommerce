namespace Shared.DataTransferObjects.Order;

public record class OrderResponseDto
{
    public Guid Id { get; init; }
    public List<OrderItemResponseDto> Items { get; init; } = new();
    public decimal TotalAmount { get; init; }
    public string? Status { get; init; }

    /// <summary>
    /// The payment provider used for this order: "Paystack" or "Flutterwave".
    /// Displayed in the order history so the user knows which gateway was used.
    /// </summary>
    public string? PaymentProvider { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
