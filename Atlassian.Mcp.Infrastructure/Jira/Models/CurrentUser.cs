namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Ответ Jira <c>rest/api/2/myself</c>: используется только для проверки доступности.
/// </summary>
public sealed record CurrentUser
{
    public string? Name { get; init; }

    public string? Key { get; init; }
}
