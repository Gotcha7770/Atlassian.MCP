namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Поля связанной задачи Jira, нужные агенту.
/// </summary>
public sealed record LinkedIssueFields
{
    public string? Summary { get; init; }
    public NamedValue? Status { get; init; }
}
