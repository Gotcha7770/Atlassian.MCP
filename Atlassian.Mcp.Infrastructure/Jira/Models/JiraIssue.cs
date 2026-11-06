namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Задача Jira в формате REST API 2.
/// </summary>
public sealed record JiraIssue
{
    public string? Id { get; init; }

    public string? Key { get; init; }

    public JiraIssueFields? Fields { get; init; }
}
