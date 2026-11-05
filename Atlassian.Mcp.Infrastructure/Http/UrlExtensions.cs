namespace Atlassian.Mcp.Infrastructure.Http;

/// <summary>
/// Работа с адресами серверов Atlassian.
/// </summary>
public static class UrlExtensions
{
    /// <summary>
    /// Базовый адрес без path: схема, хост и порт.
    /// </summary>
    public static Uri GetBaseAddress(this Uri uri)
    {
        return new UriBuilder(uri)
        {
            Path = "/",
            Query = string.Empty,
            Fragment = string.Empty
        }.Uri;
    }

    /// <summary>
    /// Context path установки: <c>/jira</c>, <c>/wiki</c> или пустая строка.
    /// </summary>
    public static string GetPathPrefix(this Uri uri) => uri.AbsolutePath.TrimEnd('/');
}
