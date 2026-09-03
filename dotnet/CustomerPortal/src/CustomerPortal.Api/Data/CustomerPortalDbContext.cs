using CustomerPortal.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomerPortal.Api.Data;

/// <summary>
/// EF Core context for the <c>customer</c> table. Every column is mapped
/// explicitly (no convention-based guessing) so the mapping stays traceable to
/// the original schema (id, email, password_hash, role, enabled, created_at,
/// updated_at).
/// </summary>
public class CustomerPortalDbContext(DbContextOptions<CustomerPortalDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customer");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(c => c.Email)
                .HasColumnName("email")
                .HasMaxLength(254)
                .IsRequired();

            entity.Property(c => c.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(c => c.Role)
                .HasColumnName("role")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(c => c.Enabled)
                .HasColumnName("enabled")
                .IsRequired();

            entity.Property(c => c.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(c => c.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.HasIndex(c => c.Email)
                .IsUnique()
                .HasDatabaseName("uq_customer_email");
        });
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>UTC audit timestamps — the .NET equivalent of Spring Data's
    /// @CreatedDate/@LastModifiedDate pinned to a UTC DateTimeProvider.</summary>
    private void ApplyAuditTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Customer>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
