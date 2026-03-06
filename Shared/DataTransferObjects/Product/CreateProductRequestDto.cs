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
    public decimal Price { get; init; } = 0;

    [Required]
    public int Stock { get; init; } = 0;

    [Required]
    [MaxLength(100)]
    public string? Category { get; init; }

    public string? ImageUrl { get; init; }
};
