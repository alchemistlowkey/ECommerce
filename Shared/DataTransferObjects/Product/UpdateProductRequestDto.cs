using System.ComponentModel.DataAnnotations;

namespace Shared.DataTransferObjects.Product;

public record UpdateProductRequestDto
{
    [MaxLength(200)]
    public string? Name { get; init; }
    public string? Description { get; init; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; init; } = 0;

    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
    public int Stock { get; init; } = 0;

    [MaxLength(100)]
    public string? Category { get; init; }
    public string? ImageUrl { get; init; }
    public bool? IsActive { get; init; }
};
