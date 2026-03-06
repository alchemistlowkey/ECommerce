using System;
using Microsoft.AspNetCore.Identity;
using Shared.DataTransferObjects.Auth;

namespace Service.Contracts;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto registerRequestDto);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto loginRequestDto);
}
