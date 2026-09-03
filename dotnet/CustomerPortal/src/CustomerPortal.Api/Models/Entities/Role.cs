namespace CustomerPortal.Api.Models.Entities;

/// <summary>
/// Permission group assigned to a <see cref="Customer"/> account. Persisted as the
/// constant name (never ordinal) — see <c>CustomerPortalDbContext</c> mapping.
/// US-001 only ever assigns <see cref="CUSTOMER"/>; <see cref="ADMIN"/> exists for
/// model completeness and is unused by this Story.
/// </summary>
public enum Role
{
    CUSTOMER,
    ADMIN
}
