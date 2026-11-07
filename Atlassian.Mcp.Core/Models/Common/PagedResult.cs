namespace Atlassian.Mcp.Core.Models.Common;

/// <summary>
/// Постраничный результат чтения коллекции.
/// </summary>
public sealed record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public int? Total { get; init; }
    public int StartAt { get; init; }
    public int LimitApplied { get; init; }
    public int? NextStart { get; init; }
    public bool Truncated { get; init; }
    public TruncationInfo? Truncation { get; init; }
}
