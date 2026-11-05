using System.Net;

namespace Atlassian.Mcp.Core.Errors;

/// <summary>
/// Ошибка взаимодействия с внешней системой Atlassian.
/// Содержит систему, HTTP Status Code и безопасное описание причины без секретов.
/// </summary>
public sealed class AtlassianApiException : Exception
{
    public const int MaxDetailsLength = 2048;

    public AtlassianApiException(
        AtlassianSystem system,
        HttpStatusCode? statusCode,
        string message,
        string? details = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        System = system;
        StatusCode = statusCode;
        Details = Truncate(details);
    }

    public AtlassianSystem System { get; }

    public HttpStatusCode? StatusCode { get; }

    public string? Details { get; }

    private static string? Truncate(string? details)
    {
        return details is null || details.Length <= MaxDetailsLength
            ? details
            : details[..MaxDetailsLength];
    }
}
