namespace CustomerPortal.Api.Security;

/// <summary>
/// Bound from the <c>Jwt</c> configuration section. <see cref="SigningKey"/> is a
/// committed local-dev secret — the same class of accepted simplification as the
/// blank SQLite/H2 dev credentials used elsewhere in this project; externalize
/// before any real deployment.
/// </summary>
public class JwtOptions
{
    public required string Issuer { get; set; }

    public required string Audience { get; set; }

    public required string SigningKey { get; set; }

    public int ExpiryMinutes { get; set; } = 60;
}
