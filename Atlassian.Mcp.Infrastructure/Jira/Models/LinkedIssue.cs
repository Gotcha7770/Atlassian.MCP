namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Связанная задача Jira (сторона связи).
/// </summary>
public sealed record LinkedIssue
{
    public string? Key { get; init; }
    public LinkedIssueFields? Fields { get; init; }
}
