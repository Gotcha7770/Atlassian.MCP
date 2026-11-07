namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Пользователь Jira.
/// </summary>
public sealed record JiraUser
{
    public string? AccountId { get; init; }
    public string? DisplayName { get; init; }
    public bool? Active { get; init; }
}
