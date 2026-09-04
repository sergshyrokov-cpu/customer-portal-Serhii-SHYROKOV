using System.Text;
using System.Text.Json.Serialization;
using CustomerPortal.Api.Data;
using CustomerPortal.Api.ErrorHandling;
using CustomerPortal.Api.Security;
using CustomerPortal.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ITokenService, TokenService>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing required 'Jwt' configuration section.");
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException(
        "Missing required 'Jwt:SigningKey' configuration value. Set it via the " +
        "Jwt__SigningKey environment variable (never commit it to appsettings.json).");
}

// Deny-by-default posture: only a request bearing a valid JWT establishes an
// identity, and the fallback policy below requires an authenticated user on
// every route that doesn't explicitly opt out with [AllowAnonymous]. JWT
// Bearer's default challenge is already a bare 401 (no login redirect), so no
// custom handler is needed to preserve that posture.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey!)),
        };
    });

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

// Quick demo UI (wwwroot) — served before routing/auth so the static
// HTML/JS/CSS itself needs no token; the pages call the JSON API, which
// still enforces [Authorize]/[AllowAnonymous] as usual. Explicit UseRouting
// here (rather than the implicit one WebApplication would otherwise insert
// at the very start of the pipeline) keeps endpoint matching — and with it
// the MapFallback auth check below — from running before static files gets
// a chance to short-circuit "/" and friends.
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

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
