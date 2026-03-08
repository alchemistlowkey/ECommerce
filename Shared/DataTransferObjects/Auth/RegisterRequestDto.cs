using System.ComponentModel.DataAnnotations;

namespace Shared.DataTransferObjects.Auth;

public record RegisterRequestDto
{
    [Required]
    [EmailAddress]
    public string? Email { get; init; }

    [Required]
    [MinLength(6)]
    public string? Password { get; init; }
}
