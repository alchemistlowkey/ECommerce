using System.ComponentModel.DataAnnotations;

namespace Shared.DataTransferObjects.Auth;

public record LoginRequestDto(
    [Required][EmailAddress] string Email,
    [Required] string Password
);
