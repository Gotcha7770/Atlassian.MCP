namespace Atlassian.Mcp.Core.Models.Jira;

/// <summary>
/// Блокирующая связь задачи Jira в ответе инструмента.
/// </summary>
public sealed record IssueLink
{
    public required string Type { get; init; }
    /// <summary>
    /// <c>outward</c> — текущая задача блокирует связанную, <c>inward</c> — заблокирована ею.
    /// </summary>
    public required string Direction { get; init; }
    public required string Key { get; init; }
    public string? Summary { get; init; }
    public string? Status { get; init; }
}
