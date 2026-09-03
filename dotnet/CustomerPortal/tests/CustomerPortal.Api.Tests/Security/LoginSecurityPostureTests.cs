using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CustomerPortal.Api.Tests.Security;

/// <summary>
/// Security-posture tests for US-002 — the deny-by-default posture must survive
/// the switch from <c>NoOpAuthenticationHandler</c> to real JWT Bearer auth:
/// login itself stays public, everyone else still gets a bare 401 without a
/// valid token, and a valid token now actually authenticates.
/// </summary>
public class LoginSecurityPostureTests : IClassFixture<CustomerPortalWebApplicationFactory>
{
    private const string LoginPath = "/api/v1/auth/login";
    private const string FallbackPath = "/api/v1/customers/999999";
    private const string ValidPassword = "Aa1!aaaaaaaa";

    private readonly CustomerPortalWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LoginSecurityPostureTests(CustomerPortalWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static string UniqueEmail(string tag) => $"posture-{tag}-{DateTime.UtcNow.Ticks}-{Guid.NewGuid():N}@example.com";

    private async Task<string> IssueTokenForNewCustomerAsync()
    {
        string email = UniqueEmail("token");
        var registerBody = new StringContent(
            JsonSerializer.Serialize(new { email, password = ValidPassword }), Encoding.UTF8, "application/json");
        var registered = await _client.PostAsync("/api/v1/customers", registerBody);
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);

        var loginBody = new StringContent(
            JsonSerializer.Serialize(new { email, password = ValidPassword }), Encoding.UTF8, "application/json");
        var loggedIn = await _client.PostAsync(LoginPath, loginBody);
        Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);

        using var json = JsonDocument.Parse(await loggedIn.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    /// Login is a public endpoint — it must not require authentication. Using
    /// valid, registered credentials isolates this from AC-002/003/004, which
    /// legitimately return 401 from the login business logic itself, not from
    /// the deny-by-default middleware blocking the route.
    [Fact]
    public async Task LoginEndpointIsReachableWithoutAuthentication()
    {
        string token = await IssueTokenForNewCustomerAsync();

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    /// Every other route stays deny-by-default without a token.
    [Fact]
    public async Task ProtectedRouteReturns401WithoutAToken()
    {
        var response = await _client.GetAsync(FallbackPath);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// A garbage bearer token must not authenticate.
    [Fact]
    public async Task ProtectedRouteReturns401WithAGarbageToken()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, FallbackPath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// A token issued by login now actually authenticates: the request passes
    /// the authorization fallback policy and reaches the (nonexistent) route,
    /// yielding 404 rather than 401.
    [Fact]
    public async Task ValidTokenAuthenticatesAndReachesTheRoute()
    {
        string token = await IssueTokenForNewCustomerAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, FallbackPath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
