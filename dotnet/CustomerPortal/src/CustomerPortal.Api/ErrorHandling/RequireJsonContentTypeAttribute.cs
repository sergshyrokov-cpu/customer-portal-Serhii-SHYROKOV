using CustomerPortal.Api.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustomerPortal.Api.ErrorHandling;

/// <summary>
/// Rejects a request whose Content-Type is missing or not JSON with 415, in the
/// shared <see cref="ErrorResponse"/> shape. Runs as a resource filter — before
/// model binding — so a missing/non-JSON Content-Type never falls through to the
/// validation branch. The .NET equivalent of Spring's automatic
/// <c>HttpMediaTypeNotSupportedException</c> for <c>consumes = "application/json"</c>.
/// </summary>
public class RequireJsonContentTypeAttribute : Attribute, IAsyncResourceFilter
{
    public Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        string? contentType = context.HttpContext.Request.ContentType;

        if (contentType is null || !contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            var body = new ErrorResponse(
                DateTimeOffset.UtcNow,
                StatusCodes.Status415UnsupportedMediaType,
                "Unsupported Media Type",
                "Content-Type must be application/json.",
                context.HttpContext.Request.Path.Value ?? string.Empty);

            context.Result = new ObjectResult(body) { StatusCode = StatusCodes.Status415UnsupportedMediaType };
            return Task.CompletedTask;
        }

        return next();
    }
}
