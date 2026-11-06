namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Связанная задача Jira (сторона связи).
/// </summary>
public sealed record JiraLinkedIssue
{
    public string? Key { get; init; }

    public JiraLinkedIssueFields? Fields { get; init; }
}
