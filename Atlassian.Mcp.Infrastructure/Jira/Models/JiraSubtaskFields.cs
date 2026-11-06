namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Поля подзадачи Jira, нужные агенту.
/// </summary>
public sealed record JiraSubtaskFields
{
    public string? Summary { get; init; }

    public JiraNamedValue? Status { get; init; }
}
