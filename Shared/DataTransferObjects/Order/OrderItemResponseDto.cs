namespace Shared.DataTransferObjects.Order;

public record OrderItemResponseDto
{
    public Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Subtotal { get; init; }

}
