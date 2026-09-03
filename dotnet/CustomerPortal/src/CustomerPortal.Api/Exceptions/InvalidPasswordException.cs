namespace CustomerPortal.Api.Exceptions;

/// <summary>
/// Raised by the service-layer password-policy re-check before hashing when a
/// password reaches the service without satisfying the policy — a
/// defense-in-depth guard behind the request-layer <c>[ValidPassword]</c>
/// constraint. Domain exception only: no HTTP concept, never the submitted value.
/// Mapped to 400 by <c>ApiExceptionHandler</c>.
/// </summary>
public class InvalidPasswordException : Exception
{
    public InvalidPasswordException()
        : base("The submitted password does not meet the security policy.")
    {
    }
}
