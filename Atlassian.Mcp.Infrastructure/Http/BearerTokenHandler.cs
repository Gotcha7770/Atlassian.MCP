using System.Net.Http.Headers;

namespace Atlassian.Mcp.Infrastructure.Http;

/// <summary>
/// Добавляет заголовок <c>Authorization: Bearer</c> с Personal Access Token одной системы.
/// Экземпляр создаётся на систему, чтобы токен Jira не попадал в Confluence.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly string _token;

    public BearerTokenHandler(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        _token = token;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return base.SendAsync(request, cancellationToken);
    }
}
