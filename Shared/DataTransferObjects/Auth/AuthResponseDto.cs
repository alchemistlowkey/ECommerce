namespace Shared.DataTransferObjects.Auth;

public record AuthResponseDto(string Token, string Email, string Role);
