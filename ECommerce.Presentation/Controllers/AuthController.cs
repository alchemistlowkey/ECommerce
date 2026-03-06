using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DataTransferObjects.Auth;

namespace ECommerce.Presentation.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IServiceManager _service;

        public AuthController(IServiceManager service)
        {
            _service = service;
        }

        /// <summary>Register a new customer account.</summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var response = await _service.Auth.RegisterAsync(request);
            return Ok(response);
        }

        /// <summary>Login and receive a JWT token.</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var response = await _service.Auth.LoginAsync(request);

            return Ok(response);
        }
    }
}
