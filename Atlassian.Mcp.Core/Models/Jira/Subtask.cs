namespace Atlassian.Mcp.Core.Models.Jira;

/// <summary>
/// Подзадача Jira в ответе инструмента.
/// </summary>
public sealed record Subtask
{
    public required string Key { get; init; }

    public string? Summary { get; init; }

    public string? Status { get; init; }
}
