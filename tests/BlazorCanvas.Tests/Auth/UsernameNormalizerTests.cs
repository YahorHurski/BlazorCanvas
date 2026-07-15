using BlazorCanvas.Auth;

namespace BlazorCanvas.Tests.Auth;

public class UsernameNormalizerTests
{
    [Theory]
    [InlineData("Egor", "egor")]
    [InlineData("egor", "egor")]
    [InlineData("  egor  ", "egor")]
    [InlineData("  EGOR ", "egor")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void Normalize_ProducesCanonicalForm(string? input, string expected)
    {
        Assert.Equal(expected, UsernameNormalizer.Normalize(input));
    }
}
