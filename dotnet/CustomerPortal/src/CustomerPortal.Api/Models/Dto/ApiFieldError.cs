namespace CustomerPortal.Api.Models.Dto;

/// <summary>
/// One entry of <see cref="ErrorResponse.FieldErrors"/> — serializes to
/// <c>{ field, message }</c>. <c>Message</c> is a safe validation message and never
/// echoes the submitted value.
/// </summary>
public record ApiFieldError(string Field, string Message);
