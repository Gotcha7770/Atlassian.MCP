using Atlassian.MCP.Server.Localization;
using Atlassian.MCP.Server.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Atlassian.Mcp.Tests.Tools;

public sealed class ToolRegistrationTests
{
    [Fact]
    public void Creates_tool_with_name_from_attribute_and_localized_description()
    {
        var resources = new ResourceAccessor("ru");

        var tools = ToolRegistration.Create([typeof(FakeJiraTools)], _ => new FakeJiraTools(), resources);

        tools.Should()
            .ContainSingle()
            .Which.ProtocolTool.Should()
            .Satisfy<Tool>(tool =>
            {
                tool.Name.Should().Be("jira_get_issue");
                tool.Description.Should().StartWith("Получить задачу Jira");
            });
    }

    [Fact]
    public void Default_language_is_english()
    {
        var resources = new ResourceAccessor("en");

        var tools = ToolRegistration.Create([typeof(FakeJiraTools)], _ => new FakeJiraTools(), resources);

        tools.Should()
            .ContainSingle()
            .Which.ProtocolTool.Description.Should()
            .StartWith("Get a Jira issue");
    }

    [McpServerToolType]
    private sealed class FakeJiraTools
    {
        [McpServerTool(Name = "jira_get_issue", ReadOnly = true)]
        public string GetIssue() => "ok";
    }
}
