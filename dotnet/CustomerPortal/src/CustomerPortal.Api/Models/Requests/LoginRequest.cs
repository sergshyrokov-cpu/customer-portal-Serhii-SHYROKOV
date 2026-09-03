using CustomerPortal.Api.Validation;

namespace CustomerPortal.Api.Models.Requests;

/// <summary>
/// Inbound body of <c>POST /api/v1/auth/login</c>. Unlike <see cref="RegistrationRequest"/>,
/// the password carries no format validation here — a malformed password must
/// fail the same way a wrong one does (generic 401), not leak policy details
/// through a 400.
/// </summary>
public record LoginRequest(
    [NotBlank]
    string Email,

    [NotBlank]
    string Password);
