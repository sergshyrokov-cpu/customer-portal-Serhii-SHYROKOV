using CustomerPortal.Api.Data;
using CustomerPortal.Api.Exceptions;
using CustomerPortal.Api.Models.Dto;
using CustomerPortal.Api.Models.Entities;
using CustomerPortal.Api.Models.Requests;
using CustomerPortal.Api.Validation;
using Microsoft.EntityFrameworkCore;

namespace CustomerPortal.Api.Services;

/// <summary>
/// Registration business logic. Normalizes the email to lowercase, re-checks the
/// password policy in bytes before hashing (defense in depth), rejects a
/// duplicate email, BCrypt-hashes the password, and persists an enabled CUSTOMER
/// account with UTC audit timestamps. Entity &lt;-&gt; DTO mapping is done here.
/// </summary>
public class CustomerService(CustomerPortalDbContext db) : ICustomerService
{
    public async Task<CustomerResponse> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken)
    {
        string email = NormalizeEmail(request.Email);
        string password = request.Password;

        if (!PasswordPolicy.IsCompliant(password))
        {
            throw new InvalidPasswordException();
        }

        if (await db.Customers.AnyAsync(c => c.Email == email, cancellationToken))
        {
            throw new DuplicateEmailException();
        }

        var customer = new Customer
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = Role.CUSTOMER,
            Enabled = true,
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(customer);
    }

    public async Task<CustomerResponse> GetProfileAsync(long requestedId, long callerId, CancellationToken cancellationToken)
    {
        if (requestedId != callerId)
        {
            throw new ProfileAccessDeniedException();
        }

        Customer? customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == requestedId, cancellationToken);

        if (customer is null)
        {
            throw new ProfileAccessDeniedException();
        }

        return ToResponse(customer);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static CustomerResponse ToResponse(Customer customer) =>
        new(customer.Id, customer.Email, customer.Role.ToString(), customer.CreatedAt);
}
