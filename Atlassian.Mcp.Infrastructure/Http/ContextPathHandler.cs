namespace Atlassian.Mcp.Infrastructure.Http;

/// <summary>
/// Добавляет context path к путям запросов, чтобы Jira и Confluence Data Center
/// работали под путями вида <c>/jira</c> и <c>/wiki</c>.
/// </summary>
public sealed class ContextPathHandler : DelegatingHandler
{
    private readonly string _pathPrefix;

    public ContextPathHandler(string pathPrefix)
    {
        _pathPrefix = pathPrefix.TrimEnd('/');
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_pathPrefix.Length > 0 && request.RequestUri is { } uri)
        {
            request.RequestUri = new UriBuilder(uri)
            {
                Path = _pathPrefix + uri.AbsolutePath
            }.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
