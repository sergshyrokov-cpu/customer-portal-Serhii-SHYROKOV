using System.Text.Json.Serialization;
using CustomerPortal.Api.Data;
using CustomerPortal.Api.ErrorHandling;
using CustomerPortal.Api.Security;
using CustomerPortal.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Reject unknown JSON properties with 400 — the .NET equivalent of
        // Jackson's fail-on-unknown-properties.
        options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = ApiValidationResponseFactory.Create;
});

builder.Services.AddDbContext<CustomerPortalDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CustomerPortal")));

builder.Services.AddScoped<ICustomerService, CustomerService>();

// Deny-by-default posture: no identity is ever established, and the fallback
// policy below requires an authenticated user on every route that doesn't
// explicitly opt out with [AllowAnonymous]. There is no login flow yet
// (US-002 is not implemented).
builder.Services
    .AddAuthentication(NoOpAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, NoOpAuthenticationHandler>(NoOpAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ApiExceptionHandler handles every exception (always returns true); AddProblemDetails
// is registered only because UseExceptionHandler() requires a fallback writer to exist.
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CustomerPortalDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Any route that doesn't match a controller action still requires
// authentication, matching Spring Security's anyRequest().authenticated().
app.MapFallback(() => Results.NotFound()).RequireAuthorization();

app.Run();

// Exposed for WebApplicationFactory<Program> in the test project.
public partial class Program
{
}
