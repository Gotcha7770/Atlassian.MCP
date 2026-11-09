using System.Net;
using System.Net.Mime;
using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Core.Errors;
using Atlassian.Mcp.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using static AwesomeAssertions.FluentActions;

namespace Atlassian.Mcp.IntegrationTests.Jira;

public sealed class GetIssueTests : IClassFixture<HttpClientTestFixture>
{
    private readonly HttpClientTestFixture _fixture;

    public GetIssueTests(HttpClientTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Requests_all_fields_when_list_is_not_specified()
    {
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://jira.example.com/rest/api/2/issue/PROJ-123")
            .WithQueryString("fields", "*all")
            .Respond(MediaTypeNames.Application.Json, IssueBody);

        await using var provider = _fixture.CreateProvider(CreateOptions(), jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        var issue = await client.GetIssue("PROJ-123", null, TestContext.Current.CancellationToken);

        issue.Key.Should().Be("PROJ-123");
        issue.CustomFields.Should().ContainKey("customfield_10101");
        mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public async Task Requests_explicit_field_list()
    {
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://jira.example.com/rest/api/2/issue/PROJ-123")
            .WithQueryString("fields", "summary,status")
            .Respond(MediaTypeNames.Application.Json, IssueBody);

        await using var provider = _fixture.CreateProvider(CreateOptions(), jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        await client.GetIssue(
            "PROJ-123",
            ["summary", "status"],
            TestContext.Current.CancellationToken);

        mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public async Task Keeps_context_path()
    {
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://jira.example.com/jira/rest/api/2/issue/PROJ-123")
            .Respond(MediaTypeNames.Application.Json, IssueBody);

        await using var provider = _fixture.CreateProvider(
            CreateOptions("https://jira.example.com/jira"),
            jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        var issue = await client.GetIssue("PROJ-123", null, TestContext.Current.CancellationToken);

        issue.Summary.Should().Be("CSV export");
        mock.GetMatchCount(request).Should().Be(1);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task Maps_error_status_to_api_exception(HttpStatusCode status)
    {
        var mock = new MockHttpMessageHandler();
        mock
            .When("https://jira.example.com/rest/api/2/issue/PROJ-123")
            .Respond(status, MediaTypeNames.Application.Json, """{"errorMessages":["Issue does not exist."]}""");

        await using var provider = _fixture.CreateProvider(CreateOptions(), jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        await Awaiting(() => client.GetIssue("PROJ-123", null, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.StatusCode == status)
            .Where(error => error.Details == "Issue does not exist.");
    }

    [Fact]
    public async Task Empty_response_is_reported_as_error()
    {
        var mock = new MockHttpMessageHandler();
        mock
            .When("https://jira.example.com/rest/api/2/issue/PROJ-123")
            .Respond(HttpStatusCode.NoContent);

        await using var provider = _fixture.CreateProvider(CreateOptions(), jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        await Awaiting(() => client.GetIssue("PROJ-123", null, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>();
    }

    private const string IssueBody =
        """
        {
          "key": "PROJ-123",
          "fields": {
            "summary": "CSV export",
            "customfield_10101": { "value": "Backend" }
          }
        }
        """;

    private static AtlassianOptions CreateOptions(string jiraUrl = "https://jira.example.com") =>
        new()
        {
            JiraUrl = new Uri(jiraUrl),
            JiraPat = "jira-token",
            ConfluenceUrl = new Uri("https://confluence.example.com"),
            ConfluencePat = "confluence-token"
        };
}