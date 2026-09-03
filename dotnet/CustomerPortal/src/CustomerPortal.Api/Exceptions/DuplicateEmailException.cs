namespace CustomerPortal.Api.Exceptions;

/// <summary>
/// Raised by the service when a registration targets an email that already has an
/// account (compared case-insensitively). Domain exception only — carries no HTTP
/// concept and no submitted value. Mapped to 409 by <c>ApiExceptionHandler</c>.
/// </summary>
public class DuplicateEmailException : Exception
{
    public DuplicateEmailException()
        : base("A customer account already exists for the requested email.")
    {
    }
}
