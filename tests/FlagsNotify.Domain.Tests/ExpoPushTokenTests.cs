using FlagsNotify.Domain;
using FlagsNotify.Domain.ValueObjects;

namespace FlagsNotify.Domain.Tests;

public sealed class ExpoPushTokenTests
{
    [Theory]
    [InlineData("ExpoPushToken[abc123]")]
    [InlineData("ExponentPushToken[abc123]")]
    public void Create_WithSupportedToken_ReturnsValueObject(string rawToken)
    {
        ExpoPushToken token = ExpoPushToken.Create(rawToken);

        Assert.Equal(rawToken, token.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc123")]
    [InlineData("ExpoPushToken[abc123")]
    public void Create_WithInvalidToken_ThrowsDomainValidation(string rawToken)
    {
        Assert.Throws<DomainValidationException>(
            () => ExpoPushToken.Create(rawToken));
    }
}
