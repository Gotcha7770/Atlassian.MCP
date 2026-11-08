using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Atlassian.Mcp.Core.Common;
using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Core.Errors;
using Atlassian.Mcp.Core.Models.Jira;
using Atlassian.Mcp.Infrastructure.Http;
using Atlassian.Mcp.Infrastructure.Jira;
using Atlassian.MCP.Server.Tools;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using RichardSzalay.MockHttp;
using static AwesomeAssertions.FluentActions;

namespace Atlassian.Mcp.IntegrationTests.Contracts;

public sealed class JiraGetIssueContractTests
{
    [Fact]
    public void Tool_exposes_contract_name_description_and_parameters()
    {
        var tool = CreateTool(new StubJiraClient());

        tool.ProtocolTool.Name.Should().Be("jiraGetIssue");
        tool.ProtocolTool.Description.Should().Be(
            "Get a Jira issue by key: summary, description, status, type, project, people, dates and custom fields.");

        var schema = tool.ProtocolTool.InputSchema;
        var properties = schema.GetProperty("properties");

        properties.GetProperty("issueKey").GetProperty("type").GetString().Should().Be("string");

        var fields = properties.GetProperty("fields");
        fields.GetProperty("type").GetRawText().Should().Contain("array");
        fields.GetProperty("items").GetProperty("type").GetRawText().Should().Contain("string");

        var required = schema.GetProperty("required")
            .EnumerateArray()
            .Select(parameter => parameter.GetString())
            .ToArray();

        required.Should().Contain("issueKey");
        required.Should().NotContain("fields");
    }

    [Fact]
    public async Task Returns_details_from_client()
    {
        var tools = new JiraTools(new StubJiraClient { Description = "CSV export" });

        var issue = await tools.GetIssue("PROJ-123", null, TestContext.Current.CancellationToken);

        issue.Key.Should().Be("PROJ-123");
        issue.Description.Should().Be("CSV export");
    }

    [Fact]
    public async Task Truncates_long_description()
    {
        var description = new string('x', ResponseLimiter.MaxTextBytes + 100);
        var tools = new JiraTools(new StubJiraClient { Description = description });

        var issue = await tools.GetIssue("PROJ-123", null, TestContext.Current.CancellationToken);

        issue.Description.Should().HaveLength(ResponseLimiter.MaxTextBytes);
        issue.TruncatedProperties.Should()
            .ContainSingle()
            .Which.Should()
            .Be("description");
    }

    [Fact]
    public async Task Empty_issue_key_is_rejected_before_api_call()
    {
        var client = new StubJiraClient();
        var tools = new JiraTools(client);

        await Awaiting(() => tools.GetIssue(" ", null, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<McpException>();

        client.Calls.Should().Be(0);
    }

    [Fact]
    public void Response_uses_all_contract_property_names()
    {
        var details = new IssueResponse
        {
            Key = "PROJ-123",
            Id = "10001",
            Summary = "CSV export",
            Description = "Export the current filter to CSV.",
            Status = "In Progress",
            IssueType = "Story",
            ProjectKey = "PROJ",
            Assignee = "Ivan Petrov",
            Reporter = "Maria Sidorova",
            Priority = "High",
            Created = "2026-10-01T09:12:00.000+0000",
            Updated = "2026-11-03T16:40:11.000+0000",
            Labels = ["reporting"],
            Subtasks =
            [
                new Subtask { Key = "PROJ-94", Summary = "Create endpoint", Status = "Done" }
            ],
            Links =
            [
                new IssueLink
                {
                    Type = "Blocks",
                    Direction = "outward",
                    Key = "PROJ-200",
                    Summary = "Release",
                    Status = "To Do"
                }
            ],
            CustomFields = new Dictionary<string, JsonElement>
            {
                ["customfield_10101"] = JsonDocument.Parse("\"Backend\"").RootElement
            },
            TruncatedProperties = ["description"]
        };

        var json = JsonSerializer.Serialize(details, JsonSerializerOptions.Web);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var expectedProperties = new[]
        {
            "key", "id", "summary", "description", "status", "issueType", "projectKey",
            "assignee", "reporter", "priority", "created", "updated", "labels",
            "subtasks", "links", "customFields", "truncatedProperties"
        };

        foreach (var property in expectedProperties)
        {
            root.TryGetProperty(property, out _).Should().BeTrue($"contract requires '{property}'");
        }

        var subtask = root.GetProperty("subtasks")[0];
        subtask.TryGetProperty("key", out _).Should().BeTrue();
        subtask.TryGetProperty("summary", out _).Should().BeTrue();
        subtask.TryGetProperty("status", out _).Should().BeTrue();

        var link = root.GetProperty("links")[0];
        link.TryGetProperty("type", out _).Should().BeTrue();
        link.TryGetProperty("direction", out _).Should().BeTrue();
        link.TryGetProperty("key", out _).Should().BeTrue();
        link.TryGetProperty("summary", out _).Should().BeTrue();
        link.TryGetProperty("status", out _).Should().BeTrue();

        root.GetProperty("truncatedProperties")[0].GetString().Should().Be("description");
    }

    [Fact]
    public void Response_omits_properties_without_value()
    {
        var details = new IssueResponse { Key = "PROJ-123" };

        var json = JsonSerializer.Serialize(details, McpJsonUtilities.DefaultOptions);

        json.Should().Contain("\"key\"");
        json.Should().NotContain("null");
    }

    [Fact]
    public async Task Tool_propagates_client_error_with_contract_message()
    {
        var exception = new AtlassianApiException(
            AtlassianSystem.Jira,
            HttpStatusCode.NotFound,
            "Jira: 404 Not Found \u2014 Issue does not exist.",
            "Issue does not exist.");
        var tools = new JiraTools(new ThrowingJiraClient(exception));

        await Awaiting(() => tools.GetIssue("PROJ-404", null, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.Message == "Jira: 404 Not Found \u2014 Issue does not exist.")
            .Where(error => error.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Error_text_matches_contract()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://jira.example.com/rest/api/2/issue/PROJ-404")
            .Respond(
                HttpStatusCode.NotFound,
                MediaTypeNames.Application.Json,
                """{"errorMessages":["Issue does not exist."]}""");

        var handler = new ErrorHandler(AtlassianSystem.Jira, JiraErrorParser.ExtractMessage)
        {
            InnerHandler = mock
        };
        using var invoker = new HttpMessageInvoker(handler);

        await Awaiting(() => invoker.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "https://jira.example.com/rest/api/2/issue/PROJ-404"),
                TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.Message == "Jira: 404 Not Found \u2014 Issue does not exist.");
    }

    private static McpServerTool CreateTool(IJiraClient client)
    {
        var method = typeof(JiraTools).GetMethod(nameof(JiraTools.GetIssue))!;
        var target = new JiraTools(client);

        return McpServerTool.Create(method, target);
    }

    private sealed class StubJiraClient : IJiraClient
    {
        public string? Description { get; init; }

        public int Calls { get; private set; }

        public Task CheckAvailabilityAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IssueResponse> GetIssueAsync(
            string issueKey,
            IReadOnlyList<string>? fields,
            CancellationToken cancellationToken)
        {
            Calls++;

            return Task.FromResult(new IssueResponse
            {
                Key = issueKey,
                Description = Description
            });
        }
    }

    private sealed class ThrowingJiraClient : IJiraClient
    {
        private readonly AtlassianApiException _exception;

        public ThrowingJiraClient(AtlassianApiException exception) => _exception = exception;

        public Task CheckAvailabilityAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IssueResponse> GetIssueAsync(
            string issueKey,
            IReadOnlyList<string>? fields,
            CancellationToken cancellationToken) => Task.FromException<IssueResponse>(_exception);
    }
}

