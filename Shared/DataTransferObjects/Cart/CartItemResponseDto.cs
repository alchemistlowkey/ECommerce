namespace Shared.DataTransferObjects.Cart;

public record CartItemResponseDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? ImageUrl { get; init; }
    public string? Category { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal Subtotal { get; init; }
}
