using Microsoft.AspNetCore.Mvc;
using PostmateAPI.DTOs;
using PostmateAPI.Services;

namespace PostmateAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IJwtService jwtService, ILogger<AuthController> logger)
        {
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest("Username and password are required");
                }

                if (!_jwtService.ValidateCredentials(request.Username, request.Password))
                {
                    return Unauthorized("Invalid credentials");
                }

                var token = _jwtService.GenerateToken(request.Username);
                var response = new LoginResponse
                {
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };

                _logger.LogInformation("User {Username} logged in successfully", request.Username);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Username}", request.Username);
                return StatusCode(500, "An error occurred during login");
            }
        }
    }
}
