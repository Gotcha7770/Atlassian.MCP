namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Поля подзадачи Jira, нужные агенту.
/// </summary>
public sealed record SubtaskFields
{
    public string? Summary { get; init; }
    public NamedValue? Status { get; init; }
}
