namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Страница результатов поиска Jira REST API 2.
/// </summary>
public sealed record IssueSearchPage
{
    public int StartAt { get; init; }
    public int MaxResults { get; init; }
    public int Total { get; init; }
    public IssueSearchResultIssue[] Issues { get; init; } = [];
}