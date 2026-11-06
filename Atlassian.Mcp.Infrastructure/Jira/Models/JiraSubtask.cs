namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Подзадача Jira внутри задачи.
/// </summary>
public sealed record JiraSubtask
{
    public string? Id { get; init; }

    public string? Key { get; init; }

    public JiraSubtaskFields? Fields { get; init; }
}
