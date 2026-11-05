using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Core.Errors;

namespace Atlassian.Mcp.Infrastructure.Confluence;

/// <summary>
/// Прикладной клиент Confluence: преобразует ошибки REST API в <see cref="AtlassianApiException"/>.
/// </summary>
public sealed class ConfluenceClient : IConfluenceClient
{
    private readonly IConfluenceApi _api;

    public ConfluenceClient(IConfluenceApi api) => _api = api;

    public Task CheckAvailabilityAsync(CancellationToken cancellationToken)
    {
        return _api.GetCurrentUser(cancellationToken);
    }
}

