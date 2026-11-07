namespace Atlassian.Mcp.Infrastructure.Confluence.Models;

/// <summary>
/// Ответ Confluence <c>rest/api/user/current</c>: используется только для проверки доступности.
/// </summary>
public sealed record CurrentUser
{
    public string? Username { get; init; }

    public string? DisplayName { get; init; }
}
