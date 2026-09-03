using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CustomerPortal.Api.Tests.Registration;

/// <summary>
/// Story-level HTTP contract tests for US-001 Customer Registration — ported 1:1
/// from the Java suite's <c>CustomerRegistrationApiTest</c>. Asserts the approved
/// API contract: status codes, body shape, and the password/email policy edge
/// cases (including the UTF-8-byte boundary).
/// </summary>
public class CustomerRegistrationApiTests : IClassFixture<CustomerPortalWebApplicationFactory>
{
    private const string Path = "/api/v1/customers";

    /// 12 chars, one upper/lower/digit/special — the minimum-length compliant password.
    private const string ValidPassword = "Aa1!aaaaaaaa";

    private readonly HttpClient _client;

    public CustomerRegistrationApiTests(CustomerPortalWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static StringContent Body(string email, string password) =>
        new(JsonSerializer.Serialize(new { email, password }), Encoding.UTF8, "application/json");

    private static string UniqueEmail(string tag) => $"user-{tag}-{DateTime.UtcNow.Ticks}-{Guid.NewGuid():N}@example.com";

    // ---------------------------------------------------------------- AC-001

    [Fact]
    public async Task ValidRegistrationReturns201WithLocationAndCustomerBody()
    {
        string email = UniqueEmail("ac001");

        var response = await _client.PostAsync(Path, Body(email, ValidPassword));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Matches(@".*/api/v1/customers/\d+", response.Headers.Location!.ToString());

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.True(root.GetProperty("id").GetInt64() > 0);
        Assert.Equal(email, root.GetProperty("email").GetString());
        Assert.Equal("CUSTOMER", root.GetProperty("role").GetString());
        Assert.True(root.TryGetProperty("createdAt", out _));
    }

    [Fact]
    public async Task EmailIsStoredAndReturnedNormalisedToLowercase()
    {
        string mixedCase = $"MixedCase-{DateTime.UtcNow.Ticks}-{Guid.NewGuid():N}@Example.COM";

        var response = await _client.PostAsync(Path, Body(mixedCase, ValidPassword));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(mixedCase.ToLowerInvariant(), json.RootElement.GetProperty("email").GetString());
    }

    // ---------------------------------------------------------------- AC-005

    [Fact]
    public async Task SuccessResponseNeverExposesCredentialOrInternalState()
    {
        var response = await _client.PostAsync(Path, Body(UniqueEmail("ac005"), ValidPassword));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.False(root.TryGetProperty("password", out _));
        Assert.False(root.TryGetProperty("passwordHash", out _));
        Assert.False(root.TryGetProperty("password_hash", out _));
        Assert.False(root.TryGetProperty("enabled", out _));
        Assert.False(root.TryGetProperty("updatedAt", out _));
    }

    // ---------------------------------------------------------------- AC-002

    [Fact]
    public async Task DuplicateEmailIsRejectedCaseInsensitivelyWith409()
    {
        string email = $"dup-{DateTime.UtcNow.Ticks}-{Guid.NewGuid():N}@example.com";

        var first = await _client.PostAsync(Path, Body(email, ValidPassword));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsync(Path, Body(email.ToUpperInvariant(), ValidPassword));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        using var json = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(409, root.GetProperty("status").GetInt32());
        Assert.Equal("Conflict", root.GetProperty("error").GetString());
        Assert.Equal("An account with this email already exists.", root.GetProperty("message").GetString());
    }

    // ---------------------------------------------------------------- AC-003

    [Fact]
    public async Task MalformedEmailReturns400WithEmailFieldError()
    {
        var response = await _client.PostAsync(Path, Body("not-an-email", ValidPassword));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.Contains(root.GetProperty("fieldErrors").EnumerateArray(), e => e.GetProperty("field").GetString() == "email");
    }

    [Fact]
    public async Task BlankEmailReturns400WithEmailFieldError()
    {
        var response = await _client.PostAsync(Path, Body("", ValidPassword));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(json.RootElement.GetProperty("fieldErrors").EnumerateArray(), e => e.GetProperty("field").GetString() == "email");
    }

    [Fact]
    public async Task EmailLongerThan254CharsReturns400WithEmailFieldError()
    {
        string local = new('a', 250);
        string tooLong = local + "@example.com"; // 262 chars

        var response = await _client.PostAsync(Path, Body(tooLong, ValidPassword));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(json.RootElement.GetProperty("fieldErrors").EnumerateArray(), e => e.GetProperty("field").GetString() == "email");
    }

    // ---------------------------------------------------------------- AC-006

    [Fact]
    public Task PasswordShorterThan12CharsReturns400WithPasswordFieldError() => AssertPasswordRejected("Aa1!aaaa"); // 8 chars

    [Fact]
    public Task PasswordWithoutUppercaseReturns400WithPasswordFieldError() => AssertPasswordRejected("aa1!aaaaaaaa");

    [Fact]
    public Task PasswordWithoutLowercaseReturns400WithPasswordFieldError() => AssertPasswordRejected("AA1!AAAAAAAA");

    [Fact]
    public Task PasswordWithoutDigitReturns400WithPasswordFieldError() => AssertPasswordRejected("Aaa!aaaaaaaa");

    [Fact]
    public Task PasswordWithoutSpecialCharReturns400WithPasswordFieldError() => AssertPasswordRejected("Aa1aaaaaaaaa");

    [Fact]
    public Task BlankPasswordReturns400WithPasswordFieldError() => AssertPasswordRejected("");

    /// The 12..72 bound is measured in UTF-8 bytes, not characters.
    /// "Aa1!" + 23x"e (euro sign)" = 27 characters but 4 + 23x3 = 73 bytes — over the limit.
    [Fact]
    public async Task PasswordOver72BytesButUnder72CharsReturns400WithPasswordFieldError()
    {
        string multiByte = "Aa1!" + new string('€', 23);
        Assert.Equal(27, multiByte.Length);
        Assert.Equal(73, Encoding.UTF8.GetByteCount(multiByte));

        await AssertPasswordRejected(multiByte);
    }

    /// Boundary: a 72-byte password that meets every class rule must be accepted.
    [Fact]
    public async Task Password72CharsMeetingPolicyIsAccepted()
    {
        string password = "Aa1!" + new string('b', 68); // 72 ASCII chars = 72 bytes

        var response = await _client.PostAsync(Path, Body(UniqueEmail("pw72"), password));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task AssertPasswordRejected(string password)
    {
        var response = await _client.PostAsync(Path, Body(UniqueEmail("pwpolicy"), password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.Contains(root.GetProperty("fieldErrors").EnumerateArray(), e => e.GetProperty("field").GetString() == "password");
    }

    /// A validation message must never echo the submitted password.
    [Fact]
    public async Task PasswordValidationMessageDoesNotEchoTheSubmittedValue()
    {
        const string secret = "supersecretweak"; // lowercase-only, fails policy, distinctive

        var response = await _client.PostAsync(Path, Body(UniqueEmail("noecho"), secret));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(secret, raw);
    }

    // ---------------------------------------------------------------- AC-007

    [Fact]
    public async Task NonJsonContentTypeReturns415()
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { email = UniqueEmail("ac007"), password = ValidPassword }),
            Encoding.UTF8,
            "text/plain");

        var response = await _client.PostAsync(Path, content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task MissingContentTypeReturns415()
    {
        var content = new ByteArrayContent(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { email = UniqueEmail("ac007b"), password = ValidPassword })));
        content.Headers.ContentType = null;

        var response = await _client.PostAsync(Path, content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    // ------------------------------------------------ derived: request shape

    [Fact]
    public async Task UnknownJsonPropertyReturns400()
    {
        string withExtra = JsonSerializer.Serialize(new
        {
            email = UniqueEmail("unknown"),
            password = ValidPassword,
            role = "ADMIN",
        });

        var response = await _client.PostAsync(Path, new StringContent(withExtra, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task MalformedJsonReturns400()
    {
        var content = new StringContent("{\"email\": \"broken@example.com\", ", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(Path, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
    }

    // ------------------------------------------------ derived: AC-6 error body

    [Fact]
    public async Task ErrorBodyHasTheApiConventionShape()
    {
        var response = await _client.PostAsync(Path, Body("not-an-email", ValidPassword));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.True(root.TryGetProperty("timestamp", out _));
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.True(root.TryGetProperty("error", out _));
        Assert.True(root.TryGetProperty("message", out _));
        Assert.Equal(Path, root.GetProperty("path").GetString());
    }

    [Fact]
    public async Task ErrorBodyNeverLeaksInternals()
    {
        var content = new StringContent("{\"email\": \"broken@example.com\", ", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(Path, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string message = json.RootElement.GetProperty("message").GetString() ?? string.Empty;

        foreach (string forbidden in new[] { "Exception", "CustomerPortal.Api", "Microsoft.", "Data Source=", "SELECT ", "INSERT " })
        {
            Assert.DoesNotContain(forbidden, message);
        }
    }
}
