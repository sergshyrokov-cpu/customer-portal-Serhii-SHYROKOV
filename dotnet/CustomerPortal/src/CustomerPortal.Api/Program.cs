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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
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
