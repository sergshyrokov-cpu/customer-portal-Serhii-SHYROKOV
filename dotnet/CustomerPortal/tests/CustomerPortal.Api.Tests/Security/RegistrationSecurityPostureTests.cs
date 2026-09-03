using System.Net;
using System.Text;

namespace CustomerPortal.Api.Tests.Security;

/// <summary>
/// Security-posture tests for US-001 — ported from the Java suite's
/// <c>RegistrationSecurityPostureTest</c>. Encodes the deny-by-default posture:
/// unauthenticated access to a protected route returns 401, not a login redirect.
///
/// <para>The Java suite's H2-console-hidden test has no equivalent here: the
/// SQLite-backed EF Core stack exposes no comparable admin console surface, so
/// there's nothing to assert against.</para>
/// </summary>
public class RegistrationSecurityPostureTests : IClassFixture<CustomerPortalWebApplicationFactory>
{
    private const string Path = "/api/v1/customers";
    private const string ValidBody = "{\"email\":\"posture@example.com\",\"password\":\"Aa1!aaaaaaaa\"}";

    private readonly HttpClient _client;

    public RegistrationSecurityPostureTests(CustomerPortalWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// Registration is the one public endpoint — it must not require authentication.
    [Fact]
    public async Task RegistrationEndpointIsReachableWithoutAuthentication()
    {
        var response = await _client.PostAsync(Path, new StringContent(ValidBody, Encoding.UTF8, "application/json"));

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// Every other route stays deny-by-default.
    [Fact]
    public async Task ProtectedRouteReturns401WhenUnauthenticated()
    {
        var response = await _client.GetAsync($"{Path}/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
