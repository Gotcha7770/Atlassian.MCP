using Atlassian.Mcp.Infrastructure.Http;

namespace Atlassian.Mcp.Tests.Http;

public sealed class UrlExtensionsTests
{
    [Theory]
    [InlineData("https://jira.example.com/jira", "https://jira.example.com/")]
    [InlineData("https://jira.example.com/jira/", "https://jira.example.com/")]
    [InlineData("https://jira.example.com", "https://jira.example.com/")]
    [InlineData("https://confluence.example.com/wiki", "https://confluence.example.com/")]
    public void GetBaseAddress_strips_path(string url, string expected)
    {
        var uri = new Uri(url);

        var baseAddress = uri.GetBaseAddress();

        baseAddress.Should().Be(new Uri(expected));
    }

    [Theory]
    [InlineData("https://jira.example.com/jira", "/jira")]
    [InlineData("https://jira.example.com/jira/", "/jira")]
    [InlineData("https://jira.example.com", "")]
    [InlineData("https://confluence.example.com/wiki", "/wiki")]
    [InlineData("https://example.com/a/b", "/a/b")]
    public void GetPathPrefix_returns_context_path(string url, string expected)
    {
        var uri = new Uri(url);

        var prefix = uri.GetPathPrefix();

        prefix.Should().Be(expected);
    }
}
