using System.Text.Json;
using Atlassian.Mcp.Core.Models.Common;

namespace Atlassian.Mcp.Core.Models.Jira;

/// <summary>
/// Задача Jira в виде, пригодном для MCP-инструмента.
/// </summary>
public sealed record IssueDetails
{
    public required string Key { get; init; }
    public string? Id { get; init; }
    public string? Summary { get; init; }
    public string? Description { get; init; }
    public string? Status { get; init; }
    public string? IssueType { get; init; }
    public string? ProjectKey { get; init; }
    public string? Assignee { get; init; }
    public string? Reporter { get; init; }
    public string? Priority { get; init; }
    public string? Created { get; init; }
    public string? Updated { get; init; }
    public string[]? Labels { get; init; }
    public IReadOnlyList<Subtask>? Subtasks { get; init; }
    public IReadOnlyList<IssueLink>? Links { get; init; }
    public Dictionary<string, JsonElement>? CustomFields { get; init; }
    public bool Truncated { get; init; }
    public IReadOnlyList<TruncationInfo> Truncations { get; init; } = [];
}
