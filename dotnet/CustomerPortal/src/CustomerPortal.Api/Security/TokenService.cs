using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CustomerPortal.Api.Models.Entities;
using CustomerPortal.Api.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CustomerPortal.Api.Security;

/// <summary>
/// Issues a signed HS256 JWT for an authenticated customer. Claims carry exactly
/// what a future authorized route needs to identify the caller: customer id
/// (<see cref="ClaimTypes.NameIdentifier"/>), email, and role.
/// </summary>
public class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public (string AccessToken, int ExpiresInSeconds) IssueToken(Customer customer)
    {
        var expiry = TimeSpan.FromMinutes(_options.ExpiryMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey!));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
            new Claim(ClaimTypes.Email, customer.Email),
            new Claim(ClaimTypes.Role, customer.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiry),
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), (int)expiry.TotalSeconds);
    }
}
