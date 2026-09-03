using System.ComponentModel.DataAnnotations;

namespace CustomerPortal.Api.Validation;

/// <summary>
/// Rejects null, empty, and whitespace-only strings — the .NET equivalent of
/// Bean Validation's <c>@NotBlank</c>, which <c>[Required]</c> alone does not cover
/// (it only rejects null/empty, not whitespace).
/// </summary>
public class NotBlankAttribute : ValidationAttribute
{
    public NotBlankAttribute()
        : base("must not be blank")
    {
    }

    public override bool IsValid(object? value) => value is string s && !string.IsNullOrWhiteSpace(s);
}
