using CustomerPortal.Api.Exceptions;
using CustomerPortal.Api.Models.Dto;
using Microsoft.AspNetCore.Diagnostics;

namespace CustomerPortal.Api.ErrorHandling;

/// <summary>
/// The single place that maps unhandled/domain exceptions to HTTP responses (the
/// .NET equivalent of Spring's <c>@RestControllerAdvice</c> for exception types —
/// validation/malformed-body responses are built separately by
/// <see cref="ApiValidationResponseFactory"/>). Every response is an
/// <see cref="ErrorResponse"/>; message is client-safe and never contains stack
/// traces, SQL, or internal type names.
/// </summary>
public class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, error, message) = exception switch
        {
            DuplicateEmailException => (StatusCodes.Status409Conflict, "Conflict", "An account with this email already exists."),
            InvalidPasswordException => (StatusCodes.Status400BadRequest, "Bad Request", "The submitted password does not meet the security policy."),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Unauthorized", "Invalid email or password."),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred."),
        };

        var body = new ErrorResponse(
            DateTimeOffset.UtcNow,
            status,
            error,
            message,
            httpContext.Request.Path.Value ?? string.Empty);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);
        return true;
    }
}
