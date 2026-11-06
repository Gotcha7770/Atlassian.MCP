using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Core.Errors;
using Microsoft.Extensions.Logging;

namespace Atlassian.Mcp.Infrastructure.Confluence;

/// <summary>
/// Прикладной клиент Confluence: преобразует ошибки REST API в <see cref="AtlassianApiException"/>.
/// </summary>
public sealed class ConfluenceClient : IConfluenceClient
{
    private readonly IConfluenceApi _api;
    private readonly ILogger<ConfluenceClient> _logger;

    public ConfluenceClient(IConfluenceApi api, ILogger<ConfluenceClient> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task CheckAvailabilityAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "{System}: {Operation} started",
            AtlassianSystem.Confluence,
            nameof(CheckAvailabilityAsync));

        try
        {
            await _api.GetCurrentUser(cancellationToken);
        }
        catch (AtlassianApiException exception)
        {
            _logger.LogError(
                exception,
                "{System}: {Operation} failed with {StatusCode}",
                AtlassianSystem.Confluence,
                nameof(CheckAvailabilityAsync),
                exception.StatusCode is { } status ? (int)status : null);

            throw;
        }

        _logger.LogInformation(
            "{System}: {Operation} completed",
            AtlassianSystem.Confluence,
            nameof(CheckAvailabilityAsync));
    }
}
