namespace CustomerPortal.Api.Models.Entities;

/// <summary>
/// A registered customer account — table <c>customer</c>. Never serialized as an
/// API request/response type directly; mapping lives in
/// <c>CustomerPortalDbContext.OnModelCreating</c>.
/// </summary>
public class Customer
{
    public long Id { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public Role Role { get; set; }

    public bool Enabled { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
