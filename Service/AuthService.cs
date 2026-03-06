using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Entities.ConfigurationModels;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Service.Contracts;
using Shared.DataTransferObjects.Auth;

namespace Service;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly JwtConfiguration _jwtConfig;

    public AuthService(UserManager<User> userManager, IOptions<JwtConfiguration> jwtConfig)
    {
        _userManager = userManager;
        _jwtConfig   = jwtConfig.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        var user = new User
        {
            UserName  = request.Email.ToLower().Trim(),
            Email     = request.Email.ToLower().Trim(),
            Role      = "Customer",
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Registration failed: {errors}");
        }

        var token = GenerateToken(user);
        return new AuthResponseDto(token, user.Email!, user.Role);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = GenerateToken(user);
        return new AuthResponseDto(token, user.Email!, user.Role);
    }

    private string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer            : _jwtConfig.ValidIssuer,
            audience          : _jwtConfig.ValidAudience,
            claims            : claims,
            expires           : DateTime.Now.AddMinutes(_jwtConfig.Expires), // "expires": 5 = 5 minutes
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}