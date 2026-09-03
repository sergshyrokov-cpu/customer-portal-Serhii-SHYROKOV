using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CustomerPortal.Api.Data;
using CustomerPortal.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerPortal.Api.Tests.Login;

/// <summary>
/// Story-level HTTP contract tests for US-002 Customer Login. Asserts the login
/// contract: status codes, body shape, and — the story's core security
/// requirement — that AC-002/003/004 are all indistinguishable from the outside.
/// </summary>
public class CustomerLoginApiTests : IClassFixture<CustomerPortalWebApplicationFactory>
{
    private const string RegisterPath = "/api/v1/customers";
    private const string LoginPath = "/api/v1/auth/login";
    private const string ValidPassword = "Aa1!aaaaaaaa";
    private const string GenericInvalidCredentialsMessage = "Invalid email or password.";

    private readonly CustomerPortalWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomerLoginApiTests(CustomerPortalWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static StringContent Body(string email, string password) =>
        new(JsonSerializer.Serialize(new { email, password }), Encoding.UTF8, "application/json");

    private static string UniqueEmail(string tag) => $"login-{tag}-{DateTime.UtcNow.Ticks}-{Guid.NewGuid():N}@example.com";

    private async Task<string> RegisterAsync(string email, string password = ValidPassword)
    {
        var response = await _client.PostAsync(RegisterPath, Body(email, password));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return email;
    }

    /// Registration always creates an enabled account; disable it directly via
    /// the DbContext to exercise AC-004, which has no HTTP path of its own.
    private async Task DisableAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerPortalDbContext>();
        Customer customer = await db.Customers.SingleAsync(c => c.Email == email.ToLowerInvariant());
        customer.Enabled = false;
        await db.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- AC-001

    [Fact]
    public async Task ValidCredentialsReturn200WithAccessTokenAndCustomer()
    {
        string email = await RegisterAsync(UniqueEmail("ac001"));

        var response = await _client.PostAsync(LoginPath, Body(email, ValidPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("accessToken").GetString()));
        Assert.Equal("Bearer", root.GetProperty("tokenType").GetString());
        Assert.True(root.GetProperty("expiresInSeconds").GetInt32() > 0);
        Assert.Equal(email, root.GetProperty("customer").GetProperty("email").GetString());
    }

    // ---------------------------------------------------------------- AC-002

    [Fact]
    public async Task WrongPasswordReturns401WithGenericMessage()
    {
        string email = await RegisterAsync(UniqueEmail("ac002"));

        var response = await _client.PostAsync(LoginPath, Body(email, "WrongPass1!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(GenericInvalidCredentialsMessage, json.RootElement.GetProperty("message").GetString());
    }

    // ---------------------------------------------------------------- AC-003

    [Fact]
    public async Task UnknownAccountReturns401WithTheSameGenericMessage()
    {
        var response = await _client.PostAsync(LoginPath, Body(UniqueEmail("ac003-unknown"), ValidPassword));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(GenericInvalidCredentialsMessage, json.RootElement.GetProperty("message").GetString());
    }

    // ---------------------------------------------------------------- AC-004

    [Fact]
    public async Task DisabledAccountReturns401WithTheSameGenericMessage()
    {
        string email = await RegisterAsync(UniqueEmail("ac004"));
        await DisableAsync(email);

        var response = await _client.PostAsync(LoginPath, Body(email, ValidPassword));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(GenericInvalidCredentialsMessage, json.RootElement.GetProperty("message").GetString());
    }

    // ---------------------------------------------------------------- AC-005

    [Fact]
    public async Task FailureResponsesAreIndistinguishableAcrossAllThreeReasons()
    {
        string enabledEmail = await RegisterAsync(UniqueEmail("ac005-enabled"));
        string disabledEmail = await RegisterAsync(UniqueEmail("ac005-disabled"));
        await DisableAsync(disabledEmail);

        var wrongPassword = await _client.PostAsync(LoginPath, Body(enabledEmail, "WrongPass1!"));
        var unknownAccount = await _client.PostAsync(LoginPath, Body(UniqueEmail("ac005-unknown"), ValidPassword));
        var disabledAccount = await _client.PostAsync(LoginPath, Body(disabledEmail, ValidPassword));

        foreach (var response in new[] { wrongPassword, unknownAccount, disabledAccount })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        string[] bodies =
        [
            await wrongPassword.Content.ReadAsStringAsync(),
            await unknownAccount.Content.ReadAsStringAsync(),
            await disabledAccount.Content.ReadAsStringAsync(),
        ];

        var messages = bodies
            .Select(b => JsonDocument.Parse(b).RootElement.GetProperty("message").GetString())
            .Distinct()
            .ToArray();

        Assert.Single(messages);
        Assert.Equal(GenericInvalidCredentialsMessage, messages[0]);
    }

    [Fact]
    public async Task SuccessResponseNeverExposesCredentialOrInternalState()
    {
        string email = await RegisterAsync(UniqueEmail("ac005-success"));

        var response = await _client.PostAsync(LoginPath, Body(email, ValidPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ValidPassword, raw);
        using var json = JsonDocument.Parse(raw);
        var customer = json.RootElement.GetProperty("customer");
        Assert.False(customer.TryGetProperty("password", out _));
        Assert.False(customer.TryGetProperty("passwordHash", out _));
        Assert.False(customer.TryGetProperty("password_hash", out _));
    }

    // ------------------------------------------------ derived: request shape

    [Fact]
    public async Task BlankEmailReturns400WithEmailFieldError()
    {
        var response = await _client.PostAsync(LoginPath, Body("", ValidPassword));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(json.RootElement.GetProperty("fieldErrors").EnumerateArray(), e => e.GetProperty("field").GetString() == "email");
    }

    [Fact]
    public async Task BlankPasswordReturns400WithPasswordFieldError()
    {
        var response = await _client.PostAsync(LoginPath, Body(UniqueEmail("blankpw"), ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(json.RootElement.GetProperty("fieldErrors").EnumerateArray(), e => e.GetProperty("field").GetString() == "password");
    }

    [Fact]
    public async Task NonJsonContentTypeReturns415()
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { email = UniqueEmail("415"), password = ValidPassword }),
            Encoding.UTF8,
            "text/plain");

        var response = await _client.PostAsync(LoginPath, content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }
}
