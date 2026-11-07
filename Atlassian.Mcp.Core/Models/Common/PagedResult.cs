namespace Atlassian.Mcp.Core.Models.Common;

/// <summary>
/// Постраничный результат чтения коллекции.
/// </summary>
/// <remarks>
/// Пока не используется: зарезервирован для инструментов пагинации US2 и US4
/// (<c>jiraSearchIssues</c>, <c>confluenceSearch</c>, <c>confluenceGetPageChildren</c>).
/// </remarks>
public sealed record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public int? Total { get; init; }
    public int StartAt { get; init; }
    public int LimitApplied { get; init; }
    public int? NextStart { get; init; }
}
