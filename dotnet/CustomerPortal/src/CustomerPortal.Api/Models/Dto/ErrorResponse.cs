using System.Text.Json.Serialization;

namespace CustomerPortal.Api.Models.Dto;

/// <summary>
/// Standard error body, produced only by the global exception handling
/// (<c>ApiExceptionHandler</c> / <c>ApiValidationResponseFactory</c>).
///
/// <para><see cref="FieldErrors"/> is omitted from the JSON when null (callers
/// never pass an empty, non-null list). <c>Message</c> is client-safe and never
/// leaks internals.</para>
/// </summary>
public record ErrorResponse(
    DateTimeOffset Timestamp,
    int Status,
    string Error,
    string Message,
    string Path,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<ApiFieldError>? FieldErrors = null);
