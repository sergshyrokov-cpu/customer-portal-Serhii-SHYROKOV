namespace CustomerPortal.Api.Security;

/// <summary>
/// Bound from the <c>Jwt</c> configuration section. <see cref="SigningKey"/> is
/// never committed — it comes from the <c>Jwt__SigningKey</c> environment
/// variable (see Program.cs, which fails fast at startup if it's absent).
/// </summary>
public class JwtOptions
{
    public required string Issuer { get; set; }

    public required string Audience { get; set; }

    public string? SigningKey { get; set; }

    public int ExpiryMinutes { get; set; } = 60;
}
