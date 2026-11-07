namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Подзадача Jira внутри задачи.
/// </summary>
public sealed record Subtask
{
    public string? Id { get; init; }
    public string? Key { get; init; }
    public SubtaskFields? Fields { get; init; }
}
