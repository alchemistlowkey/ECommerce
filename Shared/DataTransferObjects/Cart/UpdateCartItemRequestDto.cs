using System.ComponentModel.DataAnnotations;

namespace Shared.DataTransferObjects.Cart;

public record UpdateCartItemRequestDto
{
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative")]
    public int Quantity { get; init; }
}