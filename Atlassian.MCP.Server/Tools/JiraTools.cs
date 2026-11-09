using System.ComponentModel;
using System.Net;
using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Core.Errors;
using Atlassian.Mcp.Core.Models.Jira;
using Atlassian.MCP.Server.Resources;
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
    public async Task<IssueResponse> GetIssue(
        [Description("Issue key, for example PROJ-123.")] string issueKey,
        [Description("Fields to return; omit to get all fields including custom fields.")] string[]? fields = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(issueKey))
        {
            throw new McpException(ToolMessages.IssueKeyRequired);
        }

        var issue = await _client.GetIssueAsync(issueKey, fields, cancellationToken);

        return IssueResponseLimiter.Limit(issue);
    }

    [McpServerTool(Name = "jiraSearchIssue", ReadOnly = true)]
    [Description("Search Jira issues using JQL and return a page of matching issues.")]
    public async Task<IssueSearchResponse> SearchIssues(
        [Description("JQL query to find Jira issues.")] string jql,
        [Description("Zero-based offset of the first issue to return.")] int startAt = 0,
        [Description("Maximum number of issues to return, from 1 to 50.")] int maxResults = 20,
        [Description("Jira fields to include; defaults to summary, status, issue type, and project.")]
        string[]? fields = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jql))
        {
            throw new McpException(ToolMessages.JqlRequired);
        }

        if (startAt < 0)
        {
            throw new McpException(ToolMessages.SearchStartAtNonNegative);
        }

        if (maxResults is < 1 or > 50)
        {
            throw new McpException(ToolMessages.SearchMaxResultsOutOfRange);
        }

        IssueSearchResponse response;

        try
        {
            response = await _client.SearchIssuesAsync(
                jql,
                startAt,
                maxResults,
                fields,
                cancellationToken);
        }
        catch (AtlassianApiException exception)
        {
            string category = GetSearchErrorCategory(exception);
            throw new McpException($"{category}: {exception.Message}");
        }

        return IssueSearchResponseLimiter.Limit(response);
    }

    private static string GetSearchErrorCategory(AtlassianApiException exception)
    {
        if (exception.StatusCode == HttpStatusCode.Forbidden)
        {
            return "permission-denied";
        }

        string details = exception.Details ?? string.Empty;

        bool hasAlternativeWording =
            details.Contains(" or ", StringComparison.OrdinalIgnoreCase)
            || details.Contains(", or ", StringComparison.OrdinalIgnoreCase);
        bool hasExistenceOrVisibilityMessage =
            details.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
            || details.Contains("doesn't exist", StringComparison.OrdinalIgnoreCase)
            || details.Contains("not found", StringComparison.OrdinalIgnoreCase)
            || details.Contains("not visible", StringComparison.OrdinalIgnoreCase);
        bool hasPermissionOrAccessMessage =
            details.Contains("permission", StringComparison.OrdinalIgnoreCase)
            || details.Contains("access", StringComparison.OrdinalIgnoreCase);
        bool hasAmbiguousPermissionMessage = hasAlternativeWording
            && hasExistenceOrVisibilityMessage
            && hasPermissionOrAccessMessage;

        if (hasAmbiguousPermissionMessage)
        {
            return "jira-error";
        }

        if (details.Contains("Error in the JQL Query", StringComparison.OrdinalIgnoreCase))
        {
            return "invalid-jql";
        }

        bool hasExplicitPermissionMessage =
            details.Contains("permission denied", StringComparison.OrdinalIgnoreCase)
            || details.Contains("permission is required", StringComparison.OrdinalIgnoreCase)
            || details.Contains("you do not have permission", StringComparison.OrdinalIgnoreCase)
            || details.Contains("you don't have permission", StringComparison.OrdinalIgnoreCase)
            || details.Contains("access denied", StringComparison.OrdinalIgnoreCase);

        return hasExplicitPermissionMessage
            ? "permission-denied"
            : "jira-error";
    }
}