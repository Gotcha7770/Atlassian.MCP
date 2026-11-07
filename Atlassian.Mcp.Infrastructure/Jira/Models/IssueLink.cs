namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Связь задачи Jira с другой задачей.
/// </summary>
public sealed record IssueLink
{
    public LinkType? Type { get; init; }

    public LinkedIssue? InwardIssue { get; init; }

    public LinkedIssue? OutwardIssue { get; init; }
}
