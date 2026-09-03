using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CustomerPortal.Api.Security;

/// <summary>
/// Registers no identity for any request. Combined with an authorization
/// fallback policy that requires an authenticated user, this makes every route
/// deny-by-default: the base <see cref="AuthenticationHandler{TOptions}"/>
/// challenge response is a bare 401, not a login redirect — the .NET equivalent
/// of Spring Security's <c>HttpStatusEntryPoint(HttpStatus.UNAUTHORIZED)</c>.
/// There is no login flow yet (US-002 is not implemented), so this is the only
/// scheme registered.
/// </summary>
public class NoOpAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "NoOp";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.NoResult());
}
