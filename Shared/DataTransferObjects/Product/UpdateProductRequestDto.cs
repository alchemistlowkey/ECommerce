using System.ComponentModel.DataAnnotations;

namespace Shared.DataTransferObjects.Product;

public record UpdateProductRequestDto
{
    [MaxLength(200)]
    public string? Name { get; init; }
    public string? Description { get; init; }
    public decimal Price { get; init; } = 0;
    public int Stock { get; init; } = 0;

    [MaxLength(100)]
    public string? Category { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; }
};
