using System.Text;

namespace CustomerPortal.Api.Validation;

/// <summary>
/// Password policy shared by <see cref="ValidPasswordAttribute"/> (request-layer
/// validation) and the service-layer re-check before hashing.
///
/// <para>Length is measured in <b>UTF-8 bytes</b>: the 72 bound is BCrypt's
/// plaintext input limit in bytes, not characters. The four character-class
/// checks operate on the string.</para>
/// </summary>
public static class PasswordPolicy
{
    private const int MinBytes = 12;
    private const int MaxBytes = 72;

    /// <summary>
    /// True when <paramref name="password"/> satisfies the full policy (12..72 UTF-8
    /// bytes, one upper/lower/digit/special). Null or empty is not compliant.
    /// </summary>
    public static bool IsCompliant(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        int bytes = Encoding.UTF8.GetByteCount(password);
        if (bytes < MinBytes || bytes > MaxBytes)
        {
            return false;
        }

        bool upper = false;
        bool lower = false;
        bool digit = false;
        bool special = false;

        foreach (char c in password)
        {
            if (char.IsUpper(c))
            {
                upper = true;
            }
            else if (char.IsLower(c))
            {
                lower = true;
            }
            else if (char.IsDigit(c))
            {
                digit = true;
            }
            else if (!char.IsLetterOrDigit(c))
            {
                special = true;
            }
        }

        return upper && lower && digit && special;
    }
}
