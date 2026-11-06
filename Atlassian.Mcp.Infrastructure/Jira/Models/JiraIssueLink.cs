namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Связь задачи Jira с другой задачей.
/// </summary>
public sealed record JiraIssueLink
{
    public JiraLinkType? Type { get; init; }

    public JiraLinkedIssue? InwardIssue { get; init; }

    public JiraLinkedIssue? OutwardIssue { get; init; }
}
