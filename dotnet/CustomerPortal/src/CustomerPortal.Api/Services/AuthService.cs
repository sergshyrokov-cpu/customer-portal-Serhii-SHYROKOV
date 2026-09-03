using CustomerPortal.Api.Data;
using CustomerPortal.Api.Exceptions;
using CustomerPortal.Api.Models.Dto;
using CustomerPortal.Api.Models.Entities;
using CustomerPortal.Api.Models.Requests;
using Microsoft.EntityFrameworkCore;

namespace CustomerPortal.Api.Services;

/// <summary>
/// Login business logic. Normalizes the email the same way registration does,
/// then rejects an unknown email, a disabled account, or a wrong password with
/// the same <see cref="InvalidCredentialsException"/> — no branch reveals which
/// reason applied.
/// </summary>
public class AuthService(CustomerPortalDbContext db, ITokenService tokenService) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        string email = NormalizeEmail(request.Email);

        Customer? customer = await db.Customers.SingleOrDefaultAsync(c => c.Email == email, cancellationToken);

        if (customer is null || !customer.Enabled || !BCrypt.Net.BCrypt.Verify(request.Password, customer.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var (accessToken, expiresInSeconds) = tokenService.IssueToken(customer);

        return new LoginResponse(accessToken, "Bearer", expiresInSeconds, ToResponse(customer));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static CustomerResponse ToResponse(Customer customer) =>
        new(customer.Id, customer.Email, customer.Role.ToString(), customer.CreatedAt);
}
