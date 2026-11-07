namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Ссылка на проект Jira внутри задачи.
/// </summary>
public sealed record ProjectRef
{
    public string? Id { get; init; }

    public string? Key { get; init; }

    public string? Name { get; init; }
}
