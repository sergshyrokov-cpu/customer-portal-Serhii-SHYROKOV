namespace CustomerPortal.Api.Exceptions;

/// <summary>
/// Raised by the login service for an unknown email, a wrong password, or a
/// disabled account — deliberately the same exception, and the same message,
/// for all three, so the HTTP response never reveals which reason applied
/// (account-enumeration prevention). Mapped to 401 by <c>ApiExceptionHandler</c>.
/// </summary>
public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }
}
