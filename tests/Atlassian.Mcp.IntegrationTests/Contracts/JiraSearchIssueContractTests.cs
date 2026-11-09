using System.Net;
using System.Text.Json;
using Atlassian.Mcp.Core.Common;
using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Core.Errors;
using Atlassian.Mcp.Core.Models.Jira;
using Atlassian.MCP.Server.Tools;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using static AwesomeAssertions.FluentActions;

namespace Atlassian.Mcp.IntegrationTests.Contracts;

public sealed class JiraSearchIssueContractTests
{
    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Error in the JQL Query: missing expression", "invalid-jql")]
    [InlineData(HttpStatusCode.Forbidden, "Browse permission is required", "permission-denied")]
    [InlineData(HttpStatusCode.Unauthorized, "Jira authentication failed", "jira-error")]
    [InlineData(HttpStatusCode.Unauthorized, "Browse permission is required", "permission-denied")]
    [InlineData(
        HttpStatusCode.BadRequest,
        "Issue not found or you do not have permission to see it.",
        "jira-error")]
    [InlineData(
        HttpStatusCode.BadRequest,
        "The project does not exist or you do not have permission to view it.",
        "jira-error")]
    public async Task Classifies_only_explicit_jira_causes_and_preserves_ambiguous_errors(
        HttpStatusCode statusCode,
        string details,
        string expectedCategory)
    {
        var exception = new AtlassianApiException(
            AtlassianSystem.Jira,
            statusCode,
            $"Jira: {(int)statusCode} {statusCode} — {details}",
            details);
        var client = new ThrowingJiraClient(exception);
        var tools = new JiraTools(client);

        await Awaiting(() => tools.SearchIssues(
                "project = PROJ",
                0,
                20,
                null,
                TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<McpException>()
            .Where(error => error.Message.StartsWith(
                $"{expectedCategory}:",
                StringComparison.Ordinal))
            .Where(error => error.Message.Contains(details, StringComparison.Ordinal))
            .Where(error => error.Message.Contains(
                ((int)statusCode).ToString(),
                StringComparison.Ordinal));

        client.SearchCalls.Should().Be(1);
    }

    [Fact]
    public void Tool_exposes_search_contract_name_and_parameters()
    {
        var response = new IssueSearchResponse
        {
            StartAt = 0,
            MaxResults = 20,
            Total = 0,
            ReturnedCount = 0,
            HasMore = false,
            NextStartAt = null,
            Issues = []
        };
        var client = new StubJiraClient(response);
        var tools = new JiraTools(client);
        var method = typeof(JiraTools).GetMethod(nameof(JiraTools.SearchIssues));
        var tool = McpServerTool.Create(method!, tools);

        tool.ProtocolTool.Name.Should().Be("jiraSearchIssue");
        var properties = tool.ProtocolTool.InputSchema.GetProperty("properties");
        properties.GetProperty("jql").GetProperty("type").GetString().Should().Be("string");
        properties.GetProperty("startAt").GetProperty("type").GetString().Should().Be("integer");
        properties.GetProperty("maxResults").GetProperty("type").GetString().Should().Be("integer");
        var fields = properties.GetProperty("fields");
        fields.GetProperty("type").GetRawText().Should().Contain("array");
        fields.GetProperty("items").GetProperty("type").GetRawText().Should().Contain("string");

        var required = tool.ProtocolTool.InputSchema.GetProperty("required")
            .EnumerateArray()
            .Select(parameter => parameter.GetString())
            .ToArray();

        required.Should().Contain("jql");
        required.Should().NotContain("startAt");
        required.Should().NotContain("maxResults");
        required.Should().NotContain("fields");
    }

    [Fact]
    public async Task Serializes_dynamic_fields_and_page_metadata_from_client()
    {
        using var statusDocument = JsonDocument.Parse(
            """{"name":"Custom workflow state","statusCategory":{"key":"review","rank":7}}""");
        using var issueTypeDocument = JsonDocument.Parse(
            """{"name":"Capability","subtask":true,"hierarchyLevel":3}""");
        using var customFieldDocument = JsonDocument.Parse("""{"value":"Backend","rank":42}""");
        var response = new IssueSearchResponse
        {
            StartAt = 0,
            MaxResults = 1,
            Total = 2,
            ReturnedCount = 1,
            HasMore = true,
            NextStartAt = 1,
            Issues =
            [
                new IssueSearchItem
                {
                    Key = "PROJ-141",
                    Id = "10141",
                    Fields = new Dictionary<string, JsonElement>
                    {
                        ["status"] = statusDocument.RootElement.Clone(),
                        ["issuetype"] = issueTypeDocument.RootElement.Clone(),
                        ["customfield_14001"] = customFieldDocument.RootElement.Clone()
                    }
                }
            ]
        };
        var client = new StubJiraClient(response);
        var tools = new JiraTools(client);

        var result = await tools.SearchIssues(
            "project = PROJ",
            0,
            1,
            ["status", "issuetype", "customfield_14001"],
            TestContext.Current.CancellationToken);
        var json = JsonSerializer.Serialize(result, McpJsonUtilities.DefaultOptions);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var issue = root.GetProperty("issues")[0];
        var issueFields = issue.GetProperty("fields");
        var status = issueFields.GetProperty("status");
        var statusCategory = status.GetProperty("statusCategory");
        bool hasMore = root.GetProperty("hasMore").GetBoolean();
        int? nextStartAt = root.GetProperty("nextStartAt").GetInt32();
        string? issueKey = issue.GetProperty("key").GetString();
        string? statusName = status.GetProperty("name").GetString();
        string? statusCategoryKey = statusCategory.GetProperty("key").GetString();
        int statusCategoryRank = statusCategory.GetProperty("rank").GetInt32();
        int issueTypeHierarchyLevel = issueFields
            .GetProperty("issuetype")
            .GetProperty("hierarchyLevel")
            .GetInt32();
        int customFieldRank = issueFields
            .GetProperty("customfield_14001")
            .GetProperty("rank")
            .GetInt32();

        hasMore.Should().BeTrue();
        nextStartAt.Should().Be(1);
        issueKey.Should().Be("PROJ-141");
        statusName.Should().Be("Custom workflow state");
        statusCategoryKey.Should().Be("review");
        statusCategoryRank.Should().Be(7);
        issueTypeHierarchyLevel.Should().Be(3);
        customFieldRank.Should().Be(42);
        client.SearchCalls.Should().Be(1);
    }

    [Fact]
    public async Task Truncates_oversized_field_before_returning_search_response()
    {
        string description = new('ж', ResponseLimiter.MaxTextBytes);
        string json = JsonSerializer.Serialize(description);
        using var document = JsonDocument.Parse(json);
        var response = new IssueSearchResponse
        {
            StartAt = 3,
            MaxResults = 1,
            Total = 5,
            ReturnedCount = 1,
            HasMore = true,
            NextStartAt = 4,
            Issues =
            [
                new IssueSearchItem
                {
                    Key = "PROJ-143",
                    Id = "10143",
                    Fields = new Dictionary<string, JsonElement>
                    {
                        ["description"] = document.RootElement.Clone()
                    }
                }
            ]
        };
        var client = new StubJiraClient(response);
        var tools = new JiraTools(client);

        var result = await tools.SearchIssues(
            "project = PROJ",
            3,
            1,
            ["description"],
            TestContext.Current.CancellationToken);
        IssueSearchItem issue = result.Issues.Should().ContainSingle().Which;
        string? returnedDescription = issue.Fields["description"].GetString();

        returnedDescription.Should().NotBeNull();
        returnedDescription!.Length.Should().BeLessThan(description.Length);
        returnedDescription.Utf8Length.Should().BeLessThanOrEqualTo(ResponseLimiter.MaxTextBytes);
        issue.Key.Should().Be("PROJ-143");
        issue.TruncatedFields.Should()
            .ContainSingle()
            .Which.Should()
            .Satisfy<TruncatedField>(truncatedField =>
            {
                truncatedField.Name.Should().Be("description");
                truncatedField.Reason.Should().Be("value-size-limit");
            });
        result.HasMore.Should().BeTrue();
        result.NextStartAt.Should().Be(4);
    }

    [Theory]
    [InlineData("", 0, 20)]
    [InlineData(" ", 0, 20)]
    [InlineData("project = PROJ", -1, 20)]
    [InlineData("project = PROJ", 0, 0)]
    [InlineData("project = PROJ", 0, 51)]
    public async Task Rejects_invalid_search_parameters_before_calling_client(
        string jql,
        int startAt,
        int maxResults)
    {
        var response = new IssueSearchResponse
        {
            StartAt = 0,
            MaxResults = 20,
            Total = 0,
            ReturnedCount = 0,
            HasMore = false,
            NextStartAt = null,
            Issues = []
        };
        var client = new StubJiraClient(response);
        var tools = new JiraTools(client);

        await Awaiting(() => tools.SearchIssues(
                jql,
                startAt,
                maxResults,
                null,
                TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<McpException>();

        client.SearchCalls.Should().Be(0);
    }

    private sealed class StubJiraClient : IJiraClient
    {
        private readonly IssueSearchResponse _response;

        public StubJiraClient(IssueSearchResponse response) => _response = response;

        public int SearchCalls { get; private set; }

        public Task CheckAvailabilityAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IssueResponse> GetIssueAsync(
            string issueKey,
            IReadOnlyList<string>? fields,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IssueSearchResponse> SearchIssuesAsync(
            string jql,
            int startAt,
            int maxResults,
            IReadOnlyList<string>? fields,
            CancellationToken cancellationToken)
        {
            SearchCalls++;

            return Task.FromResult(_response);
        }
    }

    private sealed class ThrowingJiraClient : IJiraClient
    {
        private readonly AtlassianApiException _exception;

        public ThrowingJiraClient(AtlassianApiException exception) => _exception = exception;

        public int SearchCalls { get; private set; }

        public Task CheckAvailabilityAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IssueResponse> GetIssueAsync(
            string issueKey,
            IReadOnlyList<string>? fields,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IssueSearchResponse> SearchIssuesAsync(
            string jql,
            int startAt,
            int maxResults,
            IReadOnlyList<string>? fields,
            CancellationToken cancellationToken)
        {
            SearchCalls++;

            return Task.FromException<IssueSearchResponse>(_exception);
        }
    }
}