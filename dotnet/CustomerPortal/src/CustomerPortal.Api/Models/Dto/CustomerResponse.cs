namespace CustomerPortal.Api.Models.Dto;

/// <summary>
/// 201 response body for customer registration. Contains exactly id, email, role,
/// createdAt — never a password, a password hash, enabled, or updatedAt.
/// </summary>
public record CustomerResponse(long Id, string Email, string Role, DateTimeOffset CreatedAt);
