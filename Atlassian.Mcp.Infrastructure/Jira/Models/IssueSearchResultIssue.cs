using System.Text.Json;

namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Jira Issue из ответа поиска с полями в исходном динамическом JSON-виде.
/// </summary>
public sealed record IssueSearchResultIssue
{
    public string? Id { get; init; }
    public required string Key { get; init; }
    public Dictionary<string, JsonElement> Fields { get; init; } = [];
}