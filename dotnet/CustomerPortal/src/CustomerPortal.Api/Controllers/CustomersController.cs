using System.Security.Claims;
using CustomerPortal.Api.ErrorHandling;
using CustomerPortal.Api.Models.Dto;
using CustomerPortal.Api.Models.Requests;
using CustomerPortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerPortal.Api.Controllers;

/// <summary>
/// HTTP entry point for customer registration (<c>POST /api/v1/customers</c>).
/// Binds and validates the request, delegates to <see cref="ICustomerService"/>,
/// and maps the outcome to 201 + a Location header. No business logic, no
/// DbContext access, no entity in a signature. Error responses are produced by
/// the global exception handling, not here.
/// </summary>
[ApiController]
[Route("api/v1/customers")]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpPost]
    [RequireJsonContentType]
    [AllowAnonymous]
    public async Task<ActionResult<CustomerResponse>> Register(
        [FromBody] RegistrationRequest request, CancellationToken cancellationToken)
    {
        CustomerResponse created = await customerService.RegisterAsync(request, cancellationToken);
        string location = $"{Request.Path}/{created.Id}";
        return Created(location, created);
    }

    [HttpGet("{id:long}")]
    [Authorize]
    public async Task<ActionResult<CustomerResponse>> GetProfile(long id, CancellationToken cancellationToken)
    {
        long callerId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        CustomerResponse profile = await customerService.GetProfileAsync(id, callerId, cancellationToken);
        return Ok(profile);
    }
}
