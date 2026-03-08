using System.ComponentModel.DataAnnotations;

namespace Shared.DataTransferObjects.Cart;

public record AddToCartRequestDto
{
    [Required]
    public Guid ProductId { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; init; }
};