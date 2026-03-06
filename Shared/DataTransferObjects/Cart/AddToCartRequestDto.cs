using System.ComponentModel.DataAnnotations;

namespace Shared.DataTransferObjects.Cart;

public record AddToCartRequestDto
(
    [Required] Guid ProductId,
    [Required][Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")] int Quantity
);