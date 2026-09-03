namespace CustomerPortal.Api.Exceptions;

/// <summary>
/// Raised when a customer requests a profile that isn't their own — including a
/// requested id that belongs to no one, so the response never reveals which
/// customer ids exist. Mapped to 403 by <c>ApiExceptionHandler</c>.
/// </summary>
public class ProfileAccessDeniedException : Exception
{
    public ProfileAccessDeniedException()
        : base("Access to this resource is denied.")
    {
    }
}
