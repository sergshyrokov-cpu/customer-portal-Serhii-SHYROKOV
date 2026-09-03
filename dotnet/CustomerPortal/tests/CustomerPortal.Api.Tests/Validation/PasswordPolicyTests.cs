using CustomerPortal.Api.Validation;

namespace CustomerPortal.Api.Tests.Validation;

/// <summary>Unit tests for <see cref="PasswordPolicy"/> — ported from the Java
/// suite's <c>PasswordPolicyValidatorTest</c>.</summary>
public class PasswordPolicyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NullOrEmptyIsNotCompliant(string? password)
    {
        Assert.False(PasswordPolicy.IsCompliant(password));
    }

    [Fact]
    public void MinimumLengthCompliantPasswordIsAccepted()
    {
        Assert.True(PasswordPolicy.IsCompliant("Aa1!aaaaaaaa")); // 12 chars
    }

    [Fact]
    public void TooShortPasswordIsRejected()
    {
        Assert.False(PasswordPolicy.IsCompliant("Aa1!aaaa")); // 8 chars
    }

    [Theory]
    [InlineData("aa1!aaaaaaaa")] // no uppercase
    [InlineData("AA1!AAAAAAAA")] // no lowercase
    [InlineData("Aaa!aaaaaaaa")] // no digit
    [InlineData("Aa1aaaaaaaaa")] // no special char
    public void MissingCharacterClassIsRejected(string password)
    {
        Assert.False(PasswordPolicy.IsCompliant(password));
    }

    [Fact]
    public void SeventyTwoByteBoundaryPasswordIsAccepted()
    {
        string password = "Aa1!" + new string('b', 68); // 72 ASCII chars = 72 bytes
        Assert.True(PasswordPolicy.IsCompliant(password));
    }

    [Fact]
    public void SeventyThreeBytePasswordIsRejectedEvenWhenUnder72Characters()
    {
        // "Aa1!" + 23x€ = 27 chars but 4 + 23*3 = 73 UTF-8 bytes.
        string password = "Aa1!" + new string('€', 23);
        Assert.False(PasswordPolicy.IsCompliant(password));
    }
}
