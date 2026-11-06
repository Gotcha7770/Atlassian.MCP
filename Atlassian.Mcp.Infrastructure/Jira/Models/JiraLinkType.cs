namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Тип связи Jira: например, <c>Blocks</c>.
/// </summary>
public sealed record JiraLinkType
{
    public string? Name { get; init; }

    public string? Inward { get; init; }

    public string? Outward { get; init; }
}
