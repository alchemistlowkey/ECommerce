using System.ComponentModel.DataAnnotations;

namespace Shared.DataTransferObjects.Auth;

public record LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string? Email { get; init; }

    [Required]
    public string? Password { get; init; }
}