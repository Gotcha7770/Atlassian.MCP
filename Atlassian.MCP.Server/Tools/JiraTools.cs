using System.ComponentModel;
using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Core.Models.Jira;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Atlassian.MCP.Server.Tools;

/// <summary>
/// MCP-инструменты Jira.
/// </summary>
[McpServerToolType]
public sealed class JiraTools
{
    private readonly IJiraClient _client;

    public JiraTools(IJiraClient client) => _client = client;

    [McpServerTool(Name = "jiraGetIssue", ReadOnly = true)]
    [Description("Get a Jira issue by key: summary, description, status, type, project, people, dates and custom fields.")]
    public async Task<JiraIssueDetails> GetIssue(
        [Description("Issue key, for example PROJ-123.")] string issueKey,
        [Description("Fields to return; omit to get all fields including custom fields.")] string[]? fields = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(issueKey))
        {
            throw new McpException("issueKey is required.");
        }

        var issue = await _client.GetIssueAsync(issueKey, fields, cancellationToken);

        return JiraIssueDetailsLimiter.Limit(issue);
    }
}

