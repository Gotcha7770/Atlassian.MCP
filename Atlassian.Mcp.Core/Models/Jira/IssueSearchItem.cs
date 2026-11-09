using System.Text.Json;

namespace Atlassian.Mcp.Core.Models.Jira;

/// <summary>
/// Jira Issue в результатах поиска с исходными динамическими полями.
/// </summary>
public sealed record IssueSearchItem
{
    public required string Key { get; init; }
    public string? Id { get; init; }
    public Dictionary<string, JsonElement> Fields { get; init; } = [];
    public IReadOnlyList<string> TruncatedFields { get; init; } = [];
}