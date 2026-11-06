namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Поля связанной задачи Jira, нужные агенту.
/// </summary>
public sealed record JiraLinkedIssueFields
{
    public string? Summary { get; init; }

    public JiraNamedValue? Status { get; init; }
}
