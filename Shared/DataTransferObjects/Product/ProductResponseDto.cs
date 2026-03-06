namespace Shared.DataTransferObjects.Product;

public record ProductResponseDto
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public decimal Price { get; init; } = 0;
    public int Stock { get; init; } = 0;
    public string? Category { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
};