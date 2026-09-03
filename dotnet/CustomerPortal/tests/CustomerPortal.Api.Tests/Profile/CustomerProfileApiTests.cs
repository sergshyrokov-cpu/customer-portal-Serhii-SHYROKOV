using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CustomerPortal.Api.Tests.Profile;

/// <summary>
/// Story-level HTTP contract tests for US-003 Customer Profile View. Asserts the
/// profile contract: a customer can view their own profile, and any other id —
/// real or not — is denied identically (no existence leak).
/// </summary>
public class CustomerProfileApiTests : IClassFixture<CustomerPortalWebApplicationFactory>
{
    private const string RegisterPath = "/api/v1/customers";
    private const string LoginPath = "/api/v1/auth/login";
    private const string ValidPassword = "Aa1!aaaaaaaa";
    private const string GenericAccessDeniedMessage = "Access to this resource is denied.";

    private readonly HttpClient _client;

    public CustomerProfileApiTests(CustomerPortalWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string UniqueEmail(string tag) => $"profile-{tag}-{DateTime.UtcNow.Ticks}-{Guid.NewGuid():N}@example.com";

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private async Task<(long Id, string Email, string Token)> RegisterAndLoginAsync(string tag)
    {
        string email = UniqueEmail(tag);

        var registered = await _client.PostAsync(RegisterPath, JsonBody(new { email, password = ValidPassword }));
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        using var registeredJson = JsonDocument.Parse(await registered.Content.ReadAsStringAsync());
        long id = registeredJson.RootElement.GetProperty("id").GetInt64();

        var loggedIn = await _client.PostAsync(LoginPath, JsonBody(new { email, password = ValidPassword }));
        Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);
        using var loginJson = JsonDocument.Parse(await loggedIn.Content.ReadAsStringAsync());
        string token = loginJson.RootElement.GetProperty("accessToken").GetString()!;

        return (id, email, token);
    }

    private Task<HttpResponseMessage> GetProfileAsync(long id, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{RegisterPath}/{id}");
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return _client.SendAsync(request);
    }

    // ---------------------------------------------------------------- AC-001

    [Fact]
    public async Task OwnProfileReturns200WithMatchingCustomerData()
    {
        var (id, email, token) = await RegisterAndLoginAsync("ac001");

        var response = await GetProfileAsync(id, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(id, root.GetProperty("id").GetInt64());
        Assert.Equal(email, root.GetProperty("email").GetString());
        Assert.Equal("CUSTOMER", root.GetProperty("role").GetString());
        Assert.True(root.TryGetProperty("createdAt", out _));
    }

    // ---------------------------------------------------------------- AC-002

    [Fact]
    public async Task AnotherCustomersProfileReturns403()
    {
        var (_, _, tokenA) = await RegisterAndLoginAsync("ac002-a");
        var (idB, _, _) = await RegisterAndLoginAsync("ac002-b");

        var response = await GetProfileAsync(idB, tokenA);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(GenericAccessDeniedMessage, json.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task NonexistentIdReturnsTheSame403AsARealOtherCustomer()
    {
        var (_, _, token) = await RegisterAndLoginAsync("ac002-nonexistent");

        var response = await GetProfileAsync(999_999_999L, token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(GenericAccessDeniedMessage, json.RootElement.GetProperty("message").GetString());
    }

    // ---------------------------------------------------------------- AC-003

    [Fact]
    public async Task SuccessResponseNeverExposesCredentialOrInternalState()
    {
        var (id, _, token) = await RegisterAndLoginAsync("ac003");

        var response = await GetProfileAsync(id, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.False(root.TryGetProperty("password", out _));
        Assert.False(root.TryGetProperty("passwordHash", out _));
        Assert.False(root.TryGetProperty("password_hash", out _));
        Assert.False(root.TryGetProperty("enabled", out _));
    }

    // ---------------------------------------------------------------- AC-004

    [Fact]
    public async Task ForbiddenResponseHasTheApiConventionShape()
    {
        var (_, _, token) = await RegisterAndLoginAsync("ac004");

        var response = await GetProfileAsync(999_999_998L, token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.True(root.TryGetProperty("timestamp", out _));
        Assert.Equal(403, root.GetProperty("status").GetInt32());
        Assert.Equal("Forbidden", root.GetProperty("error").GetString());
        Assert.True(root.TryGetProperty("message", out _));
        Assert.Equal($"{RegisterPath}/999999998", root.GetProperty("path").GetString());
    }

    // ------------------------------------------------ derived: deny-by-default

    [Fact]
    public async Task NoTokenReturns401()
    {
        var (id, _, _) = await RegisterAndLoginAsync("noauth");

        var response = await GetProfileAsync(id, token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
