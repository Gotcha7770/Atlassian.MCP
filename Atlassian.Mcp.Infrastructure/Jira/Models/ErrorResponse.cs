namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Тело ответа Jira с описанием ошибки.
/// </summary>
public sealed record ErrorResponse
{
    public string[]? ErrorMessages { get; init; }

    public Dictionary<string, string>? Errors { get; init; }
}
