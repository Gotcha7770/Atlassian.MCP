using Atlassian.Mcp.Core.Common;

namespace Atlassian.Mcp.Tests.Limits;

public sealed class StringExtensionsTests
{
    [Theory]
    [InlineData("Hello, world!", 5, "Hello")]
    [InlineData("Привет", 5, "Пр")]
    [InlineData("Привет", 4, "Пр")]
    [InlineData("abc😀def", 6, "abc")]
    [InlineData("abc😀def", 7, "abc😀")]
    [InlineData("😀😀", 5, "😀")]
    [InlineData("Hello", 0, "")]
    [InlineData("Hello", 100, "Hello")]
    public void Truncates_to_utf8_budget(string value, int maxBytes, string expected)
    {
        var result = value.TruncateToBytes(maxBytes);

        result.Should().Be(expected);
        result.Utf8Length.Should().BeLessThanOrEqualTo(Math.Max(maxBytes, 0));
    }

    [Fact]
    public void Returns_same_instance_when_value_fits()
    {
        var value = "Hello";

        var result = value.TruncateToBytes(value.Length);

        result.Should().BeSameAs(value);
    }
}
