using System.ComponentModel.DataAnnotations;

namespace Shared.DataTransferObjects.Product;

public record CreateProductRequestDto
{
    [Required]
    [MaxLength(200)]
    public string? Name { get; init; }

    [Required]
    public string? Description { get; init; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; init; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
    public int Stock { get; init; }

    [Required]
    [MaxLength(100)]
    public string? Category { get; init; }

    public string? ImageUrl { get; init; }
}
