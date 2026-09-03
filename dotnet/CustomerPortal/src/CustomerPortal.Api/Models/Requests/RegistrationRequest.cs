using System.ComponentModel.DataAnnotations;
using CustomerPortal.Api.Validation;

namespace CustomerPortal.Api.Models.Requests;

/// <summary>
/// Inbound body of <c>POST /api/v1/customers</c>.
///
/// <para><c>Email</c>: required, well-formed, max 254 characters. <c>Password</c>:
/// required and policy-compliant (<see cref="ValidPasswordAttribute"/>). Unknown /
/// extra JSON properties are rejected with 400 via
/// <c>JsonSerializerOptions.UnmappedMemberHandling</c> — the DTO needs no
/// extra attributes for that.</para>
/// </summary>
public record RegistrationRequest(
    [NotBlank]
    [EmailAddress]
    [StringLength(254)]
    string Email,

    [NotBlank]
    [ValidPassword]
    string Password);
