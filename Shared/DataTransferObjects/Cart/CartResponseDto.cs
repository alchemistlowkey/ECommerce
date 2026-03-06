namespace Shared.DataTransferObjects.Cart;

public record CartResponseDto
{
    public Guid Id { get; init; }
    public List<CartItemResponseDto> Items { get; init; } = new();
    public decimal Total { get; init; }
}