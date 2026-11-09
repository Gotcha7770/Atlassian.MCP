namespace Atlassian.Mcp.Core.Models.Jira;

/// <summary>
/// Jira-поле, значение которого было сокращено или исключено из результата поиска.
/// </summary>
public sealed record TruncatedField
{
    public required string Name { get; init; }
    public required string Reason { get; init; }
}