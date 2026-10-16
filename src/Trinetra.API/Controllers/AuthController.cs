using Microsoft.AspNetCore.Mvc;
using Trinetra.API.DTOs;
using Trinetra.API.Services;

namespace Trinetra.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates a user with username/email and password, returning JWT access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "BAD_REQUEST",
                    Message = "Username and password are required",
                    Status = StatusCodes.Status400BadRequest
                }
            });
        }

        var response = await _authService.LoginAsync(request, cancellationToken);
        if (response == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "UNAUTHORIZED",
                    Message = "Invalid credentials or inactive account",
                    Status = StatusCodes.Status401Unauthorized
                }
            });
        }

        return Ok(response);
    }

    /// <summary>
    /// Rotates the refresh token and returns a new access token.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(request, cancellationToken);
        if (response == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "INVALID_REFRESH_TOKEN",
                    Message = "The refresh token is invalid or expired",
                    Status = StatusCodes.Status401Unauthorized
                }
            });
        }

        return Ok(response);
    }
}
