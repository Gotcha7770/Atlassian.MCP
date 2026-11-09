using System.Net;
using System.Net.Mime;
using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Core.Errors;
using Atlassian.Mcp.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using static AwesomeAssertions.FluentActions;

namespace Atlassian.Mcp.IntegrationTests.Jira;

public sealed class SearchIssuesTests : IClassFixture<HttpClientTestFixture>
{
    private readonly HttpClientTestFixture _fixture;

    public SearchIssuesTests(HttpClientTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Requests_minimal_discovery_fields_when_fields_are_not_specified()
    {
        var jql = "project = PROJ";
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://jira.example.com/rest/api/2/search")
            .WithQueryString("jql", jql)
            .WithQueryString("startAt", "0")
            .WithQueryString("maxResults", "20")
            .WithQueryString("fields", "summary,status,issuetype,project")
            .Respond(
                MediaTypeNames.Application.Json,
                """
                {
                  "startAt": 0,
                  "maxResults": 20,
                  "total": 1,
                  "issues": [
                    {
                      "id": "10140",
                      "key": "PROJ-140",
                      "fields": {
                        "summary": "Export report",
                        "status": { "name": "Ready" },
                        "issuetype": { "name": "Task" },
                        "project": { "key": "PROJ" }
                      }
                    }
                  ]
                }
                """);
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri("https://jira.example.com"),
            JiraPat = "jira-token",
            ConfluenceUrl = new Uri("https://confluence.example.com"),
            ConfluencePat = "confluence-token"
        };

        await using var provider = _fixture.CreateProvider(options, jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        var result = await client.SearchIssuesAsync(
            jql,
            0,
            20,
            null,
            TestContext.Current.CancellationToken);

        result.Issues.Should().ContainSingle().Which.Key.Should().Be("PROJ-140");
        result.Issues[0].Fields.Should()
            .ContainKeys("summary", "status", "issuetype", "project");
        mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public async Task Uses_actual_returned_count_when_jira_clamps_max_results()
    {
        var jql = "project = PROJ AND status = \"In Progress\"";
        var fields = new[] { "summary", "customfield_14001", "workratio" };
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://jira.example.com/rest/api/2/search")
            .WithQueryString("jql", jql)
            .WithQueryString("startAt", "4")
            .WithQueryString("maxResults", "10")
            .WithQueryString("fields", "summary,customfield_14001,workratio")
            .Respond(
                MediaTypeNames.Application.Json,
                """
                {
                  "startAt": 4,
                  "maxResults": 2,
                  "total": 9,
                  "issues": [
                    {
                      "id": "10141",
                      "key": "PROJ-141",
                      "fields": {
                        "summary": "First candidate",
                        "customfield_14001": { "value": "Backend" },
                        "workratio": 31
                      }
                    },
                    {
                      "id": "10142",
                      "key": "PROJ-142",
                      "fields": {
                        "summary": "Second candidate",
                        "customfield_14001": { "value": "Platform" },
                        "workratio": 47
                      }
                    }
                  ]
                }
                """);
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri("https://jira.example.com"),
            JiraPat = "jira-token",
            ConfluenceUrl = new Uri("https://confluence.example.com"),
            ConfluencePat = "confluence-token"
        };

        await using var provider = _fixture.CreateProvider(options, jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        var result = await client.SearchIssuesAsync(
            jql,
            4,
            10,
            fields,
            TestContext.Current.CancellationToken);

        result.MaxResults.Should().Be(2);
        result.ReturnedCount.Should().Be(2);
        result.Total.Should().Be(9);
        result.HasMore.Should().BeTrue();
        result.NextStartAt.Should().Be(6);
        result.Issues[1].Fields["customfield_14001"].GetProperty("value").GetString()
            .Should().Be("Platform");
        mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public async Task Preserves_explicit_invalid_jql_error_details()
    {
        var jql = "project = PROJ AND";
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://jira.example.com/rest/api/2/search")
            .WithQueryString("jql", jql)
            .Respond(
                HttpStatusCode.BadRequest,
                MediaTypeNames.Application.Json,
                """{"errorMessages":["Error in the JQL Query: missing expression"]}""");
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri("https://jira.example.com"),
            JiraPat = "jira-token",
            ConfluenceUrl = new Uri("https://confluence.example.com"),
            ConfluencePat = "confluence-token"
        };

        await using var provider = _fixture.CreateProvider(options, jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        await Awaiting(() => client.SearchIssuesAsync(
                jql,
                0,
                20,
                null,
                TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.StatusCode == HttpStatusCode.BadRequest)
            .Where(error => error.Details == "Error in the JQL Query: missing expression");

        mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public async Task Preserves_explicit_permission_error_status_and_details()
    {
        var jql = "project = PRIVATE";
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://jira.example.com/rest/api/2/search")
            .WithQueryString("jql", jql)
            .Respond(
                HttpStatusCode.Forbidden,
                MediaTypeNames.Application.Json,
                """{"errorMessages":["Browse permission is required"]}""");
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri("https://jira.example.com"),
            JiraPat = "jira-token",
            ConfluenceUrl = new Uri("https://confluence.example.com"),
            ConfluencePat = "confluence-token"
        };

        await using var provider = _fixture.CreateProvider(options, jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        await Awaiting(() => client.SearchIssuesAsync(
                jql,
                0,
                20,
                null,
                TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.StatusCode == HttpStatusCode.Forbidden)
            .Where(error => error.Details == "Browse permission is required");

        mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public async Task Preserves_ambiguous_http_400_without_claiming_permission_denial()
    {
        var jql = "project = HIDDEN";
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://jira.example.com/rest/api/2/search")
            .WithQueryString("jql", jql)
            .Respond(
                HttpStatusCode.BadRequest,
                MediaTypeNames.Application.Json,
                """{"errorMessages":["The project does not exist or you do not have permission to view it."]}""");
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri("https://jira.example.com"),
            JiraPat = "jira-token",
            ConfluenceUrl = new Uri("https://confluence.example.com"),
            ConfluencePat = "confluence-token"
        };

        await using var provider = _fixture.CreateProvider(options, jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        await Awaiting(() => client.SearchIssuesAsync(
                jql,
                0,
                20,
                null,
                TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.StatusCode == HttpStatusCode.BadRequest)
            .Where(error => error.Details == "The project does not exist or you do not have permission to view it.");

        mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public async Task Reports_no_content_search_response_as_jira_api_error()
    {
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://jira.example.com/rest/api/2/search")
            .Respond(HttpStatusCode.NoContent);
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri("https://jira.example.com"),
            JiraPat = "jira-token",
            ConfluenceUrl = new Uri("https://confluence.example.com"),
            ConfluencePat = "confluence-token"
        };

        await using var provider = _fixture.CreateProvider(options, jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        await Awaiting(() => client.SearchIssuesAsync(
                "project = PROJ",
                0,
                20,
                null,
                TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.StatusCode == HttpStatusCode.NoContent)
            .Where(error => error.Details == null);

        mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public async Task Reports_successful_http_response_without_body_as_jira_api_error()
    {
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://jira.example.com/rest/api/2/search")
            .Respond(HttpStatusCode.OK, MediaTypeNames.Application.Json, string.Empty);
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri("https://jira.example.com"),
            JiraPat = "jira-token",
            ConfluenceUrl = new Uri("https://confluence.example.com"),
            ConfluencePat = "confluence-token"
        };

        await using var provider = _fixture.CreateProvider(options, jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        await Awaiting(() => client.SearchIssuesAsync(
                "project = PROJ",
                0,
                20,
                null,
                TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.StatusCode == HttpStatusCode.OK);

        mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public async Task Returns_empty_page_when_jira_search_succeeds_without_matches()
    {
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://jira.example.com/rest/api/2/search")
            .Respond(
                MediaTypeNames.Application.Json,
                """{"startAt":0,"maxResults":20,"total":0,"issues":[]}""");
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri("https://jira.example.com"),
            JiraPat = "jira-token",
            ConfluenceUrl = new Uri("https://confluence.example.com"),
            ConfluencePat = "confluence-token"
        };

        await using var provider = _fixture.CreateProvider(options, jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        var result = await client.SearchIssuesAsync(
            "project = PROJ",
            0,
            20,
            null,
            TestContext.Current.CancellationToken);

        result.Issues.Should().BeEmpty();
        result.HasMore.Should().BeFalse();
        result.NextStartAt.Should().BeNull();
        mock.GetMatchCount(request).Should().Be(1);
    }
}