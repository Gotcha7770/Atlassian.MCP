using System.ComponentModel.DataAnnotations;
using Atlassian.Mcp.Infrastructure.Configuration;

namespace Atlassian.Mcp.Tests.Configuration;

public sealed class AtlassianOptionsValidationTests
{
    [Fact]
    public void Valid_options_produce_no_errors()
    {
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri("https://jira.example.com/jira"),
            JiraPat = "jira-token",
            ConfluenceUrl = new Uri("https://confluence.example.com/wiki"),
            ConfluencePat = "confluence-token"
        };

        var results = Validate(options);

        results.Should().BeEmpty();
    }

    [Fact]
    public void Missing_pat_is_reported_without_value()
    {
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri("https://jira.example.com/jira"),
            JiraPat = " ",
            ConfluenceUrl = new Uri("https://confluence.example.com/wiki"),
            ConfluencePat = "confluence-token"
        };

        var results = Validate(options);

        results.Should()
            .ContainSingle()
            .Which.Should()
            .Satisfy<ValidationResult>(result =>
                result.ErrorMessage.Should().Contain(AtlassianOptions.JiraPatKey));
    }

    [Theory]
    [InlineData("jira.example.com/jira")]
    [InlineData("ftp://jira.example.com")]
    [InlineData("https://jira.example.com/jira?x=1")]
    [InlineData("https://jira.example.com/jira#section")]
    public void Invalid_jira_url_is_reported(string url)
    {
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri(url, UriKind.RelativeOrAbsolute),
            JiraPat = "token",
            ConfluenceUrl = new Uri("https://confluence.example.com/wiki"),
            ConfluencePat = "token"
        };

        var results = Validate(options);

        results.Should()
            .ContainSingle()
            .Which.Should()
            .Satisfy<ValidationResult>(result =>
                result.ErrorMessage.Should().Contain(AtlassianOptions.JiraUrlKey));
    }

    [Fact]
    public void Unsupported_language_is_reported()
    {
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri("https://jira.example.com/jira"),
            JiraPat = "token",
            ConfluenceUrl = new Uri("https://confluence.example.com/wiki"),
            ConfluencePat = "token",
            Language = "de"
        };

        var results = Validate(options);

        results.Should()
            .ContainSingle()
            .Which.Should()
            .Satisfy<ValidationResult>(result => result.ErrorMessage.Should().Contain("de"));
    }

    [Fact]
    public void Language_is_case_insensitive()
    {
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri("https://jira.example.com/jira"),
            JiraPat = "token",
            ConfluenceUrl = new Uri("https://confluence.example.com/wiki"),
            ConfluencePat = "token",
            Language = "RU"
        };

        var results = Validate(options);

        results.Should().BeEmpty();
    }

    private static IReadOnlyList<ValidationResult> Validate(AtlassianOptions options)
    {
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            results,
            validateAllProperties: true);

        return results;
    }
}

