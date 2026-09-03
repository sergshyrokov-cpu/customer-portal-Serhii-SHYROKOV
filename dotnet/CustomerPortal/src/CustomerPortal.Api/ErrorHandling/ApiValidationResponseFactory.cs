using System.Text.Json;
using CustomerPortal.Api.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CustomerPortal.Api.ErrorHandling;

/// <summary>
/// Builds the 400 <see cref="ErrorResponse"/> for both DataAnnotations validation
/// failures and unreadable request bodies (malformed JSON / unknown JSON
/// property) — the two branches Spring keeps separate as
/// <c>MethodArgumentNotValidException</c> and
/// <c>HttpMessageNotReadableException</c>. Both surface here as an invalid
/// <see cref="ActionContext.ModelState"/>; a <see cref="JsonException"/> attached
/// to a model error distinguishes an unreadable body from a field-level failure.
/// </summary>
public static class ApiValidationResponseFactory
{
    public static IActionResult Create(ActionContext context)
    {
        var modelState = context.ModelState;
        string path = context.HttpContext.Request.Path.Value ?? string.Empty;

        bool isBodyUnreadable = modelState.Values
            .SelectMany(v => v.Errors)
            .Any(e => e.Exception is JsonException);

        ErrorResponse body = isBodyUnreadable
            ? new ErrorResponse(
                DateTimeOffset.UtcNow,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "The request body is missing, malformed, or contains an unknown field.",
                path)
            : new ErrorResponse(
                DateTimeOffset.UtcNow,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "Validation failed for one or more fields.",
                path,
                BuildFieldErrors(modelState));

        return new BadRequestObjectResult(body);
    }

    private static List<ApiFieldError>? BuildFieldErrors(ModelStateDictionary modelState)
    {
        var fieldErrors = modelState
            .Where(kvp => kvp.Value is { Errors.Count: > 0 })
            .SelectMany(kvp => kvp.Value!.Errors.Select(e => new ApiFieldError(
                ToCamelCase(kvp.Key),
                string.IsNullOrEmpty(e.ErrorMessage) ? "invalid value" : e.ErrorMessage)))
            .ToList();

        return fieldErrors.Count > 0 ? fieldErrors : null;
    }

    private static string ToCamelCase(string key) =>
        string.IsNullOrEmpty(key) ? key : char.ToLowerInvariant(key[0]) + key[1..];
}
