using System.Text.Json;
using Atlassian.Mcp.Core.Common;
using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Core.Models.Common;
using Atlassian.Mcp.Core.Models.Jira;
using Atlassian.MCP.Server.Tools;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using static AwesomeAssertions.FluentActions;

namespace Atlassian.Mcp.IntegrationTests.Contracts;

public sealed class JiraGetIssueContractTests
{
    [Fact]
    public void Tool_exposes_contract_name_and_parameters()
    {
        var tool = CreateTool(new StubJiraClient());

        tool.ProtocolTool.Name.Should().Be("jiraGetIssue");

        var schema = tool.ProtocolTool.InputSchema.ToString();
        schema.Should().Contain("issueKey");
        schema.Should().Contain("fields");
    }

    [Fact]
    public async Task Returns_details_from_client()
    {
        var tools = new JiraTools(new StubJiraClient { Description = "CSV export" });

        var issue = await tools.GetIssue("PROJ-123", null, TestContext.Current.CancellationToken);

        issue.Key.Should().Be("PROJ-123");
        issue.Description.Should().Be("CSV export");
        issue.Truncated.Should().BeFalse();
    }

    [Fact]
    public async Task Truncates_long_description()
    {
        var description = new string('x', ResponseLimiter.MaxTextBytes + 100);
        var tools = new JiraTools(new StubJiraClient { Description = description });

        var issue = await tools.GetIssue("PROJ-123", null, TestContext.Current.CancellationToken);

        issue.Description.Should().HaveLength(ResponseLimiter.MaxTextBytes);
        issue.Truncated.Should().BeTrue();
        issue.Truncations.Should()
            .ContainSingle()
            .Which.Should()
            .Satisfy<TruncationInfo>(truncation =>
            {
                truncation.Field.Should().Be("description");
                truncation.OriginalLength.Should().Be(description.Length);
            });
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
    public void Response_uses_contract_property_names()
    {
        var details = new IssueDetails
        {
            Key = "PROJ-123",
            ProjectKey = "PROJ",
            CustomFields = new Dictionary<string, JsonElement>
            {
                ["customfield_10101"] = JsonDocument.Parse("\"Backend\"").RootElement
            }
        };

        var json = JsonSerializer.Serialize(details, JsonSerializerOptions.Web);

        json.Should().Contain("\"key\"");
        json.Should().Contain("\"projectKey\"");
        json.Should().Contain("\"customFields\"");
    }

    [Fact]
    public void Response_omits_properties_without_value()
    {
        var details = new IssueDetails { Key = "PROJ-123" };

        var json = JsonSerializer.Serialize(details, McpJsonUtilities.DefaultOptions);

        json.Should().Contain("\"key\"");
        json.Should().NotContain("null");
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

        public Task<IssueDetails> GetIssueAsync(
            string issueKey,
            IReadOnlyList<string>? fields,
            CancellationToken cancellationToken)
        {
            Calls++;

            return Task.FromResult(new IssueDetails
            {
                Key = issueKey,
                Description = Description
            });
        }
    }
}

