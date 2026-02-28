using Microsoft.AspNetCore.Mvc;
using RecipeSugesstionApp.DTOs;
using RecipeSugesstionApp.Services;

namespace RecipeSugesstionApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth) => _auth = auth;

        /// <summary>Register a new user account.</summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 409)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(BuildValidationError());

            var result = await _auth.RegisterAsync(dto);
            if (result is null)
                return Conflict(new ErrorResponse
                {
                    Message = "An account with this username or email already exists."
                });

            return Ok(result);
        }

        /// <summary>Login and receive a JWT Bearer token.</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(BuildValidationError());

            var result = await _auth.LoginAsync(dto);
            if (result is null)
                return Unauthorized(new ErrorResponse
                {
                    Message = "Invalid email or password."
                });

            return Ok(result);
        }

        // ── Helpers ─────────────────────────────────────────────────────────
        private ErrorResponse BuildValidationError() => new()
        {
            Message = "One or more validation errors occurred.",
            Errors  = ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray())
        };
    }
}
