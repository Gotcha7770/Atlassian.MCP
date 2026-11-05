using Atlassian.Mcp.Core.Contracts;

namespace Atlassian.Mcp.Infrastructure.Jira;

/// <summary>
/// Прикладной клиент Jira.
/// </summary>
public sealed class JiraClient : IJiraClient
{
    private readonly IJiraApi _api;

    public JiraClient(IJiraApi api) => _api = api;

    public Task CheckAvailabilityAsync(CancellationToken cancellationToken) => _api.GetCurrentUser(cancellationToken);
}

