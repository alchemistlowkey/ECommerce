namespace Shared.DataTransferObjects.Order;

public record class OrderResponseDto
{
    public Guid Id { get; init; }
    public List<OrderItemResponseDto> Items { get; init; } = new();
    public decimal TotalAmount { get; init; }
    public string? Status { get; init; }
    public string? PaymentProvider { get; init; }  // ← NEW
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}