using CustomerPortal.Api.ErrorHandling;
using CustomerPortal.Api.Models.Dto;
using CustomerPortal.Api.Models.Requests;
using CustomerPortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerPortal.Api.Controllers;

/// <summary>
/// HTTP entry point for customer login (<c>POST /api/v1/auth/login</c>). Binds
/// and validates the request, delegates to <see cref="IAuthService"/>, and
/// returns the issued token. No business logic, no DbContext access. Error
/// responses are produced by the global exception handling, not here.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [RequireJsonContentType]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        LoginResponse response = await authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }
}
