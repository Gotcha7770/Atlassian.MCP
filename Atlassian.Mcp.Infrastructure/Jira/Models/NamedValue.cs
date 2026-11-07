namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Именованная сущность Jira: статус, тип задачи, приоритет.
/// </summary>
public sealed record NamedValue
{
    public string? Id { get; init; }

    public string? Name { get; init; }
}
