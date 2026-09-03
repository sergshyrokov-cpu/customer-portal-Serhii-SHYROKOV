using System.ComponentModel.DataAnnotations;

namespace CustomerPortal.Api.Validation;

/// <summary>
/// Request-layer constraint enforcing the training-project password policy: 12..72
/// UTF-8 bytes and at least one uppercase, lowercase, digit, and special character.
/// The message is static and generic — it never echoes the submitted value.
/// Null/empty is left to <see cref="NotBlankAttribute"/>.
/// </summary>
public class ValidPasswordAttribute : ValidationAttribute
{
    public ValidPasswordAttribute()
        : base("Password does not meet the security policy.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is not string password || string.IsNullOrEmpty(password))
        {
            return true;
        }

        return PasswordPolicy.IsCompliant(password);
    }
}
