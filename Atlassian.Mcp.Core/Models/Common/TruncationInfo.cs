namespace Atlassian.Mcp.Core.Models.Common;

/// <summary>
/// Описание усечения значения в ответе инструмента.
/// </summary>
public sealed record TruncationInfo
{
    public required string Field { get; init; }

    public int OriginalLength { get; init; }

    public int AppliedLength { get; init; }
}
