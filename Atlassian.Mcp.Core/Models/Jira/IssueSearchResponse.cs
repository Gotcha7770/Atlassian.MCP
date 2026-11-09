namespace Atlassian.Mcp.Core.Models.Jira;

/// <summary>
/// Результат одной страницы поиска Jira для MCP-инструмента.
/// </summary>
public sealed record IssueSearchResponse
{
    public int StartAt { get; init; }
    public int MaxResults { get; init; }
    public int Total { get; init; }
    public int ReturnedCount { get; init; }
    public bool HasMore { get; init; }
    public int? NextStartAt { get; init; }
    public IReadOnlyList<IssueSearchItem> Issues { get; init; } = [];
}