namespace Atlassian.Mcp.Infrastructure.Confluence.Models;

/// <summary>
/// Тело ответа Confluence с описанием ошибки.
/// </summary>
public sealed record ErrorResponse
{
    public int? StatusCode { get; init; }
    public string? Message { get; init; }
}
