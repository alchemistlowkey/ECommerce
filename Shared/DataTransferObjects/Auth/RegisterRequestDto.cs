using System.ComponentModel.DataAnnotations;

namespace Shared.DataTransferObjects.Auth;

public record RegisterRequestDto(
    [Required][EmailAddress] string Email,
    [Required][MinLength(6)] string Password
);