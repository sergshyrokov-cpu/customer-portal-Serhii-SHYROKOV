namespace CustomerPortal.Api.Models.Dto;

/// <summary>
/// 200 response body for a successful login. Never contains a password or a
/// password hash — <see cref="Customer"/> reuses <see cref="CustomerResponse"/>,
/// which already excludes both.
/// </summary>
public record LoginResponse(string AccessToken, string TokenType, int ExpiresInSeconds, CustomerResponse Customer);
