using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Core.Errors;
using Atlassian.Mcp.Core.Models.Jira;

namespace Atlassian.Mcp.Infrastructure.Jira;

/// <summary>
/// Прикладной клиент Jira.
/// </summary>
public sealed class JiraClient : IJiraClient
{
    private readonly IJiraApi _api;

    public JiraClient(IJiraApi api) => _api = api;

    public Task CheckAvailabilityAsync(CancellationToken cancellationToken) => _api.GetCurrentUser(cancellationToken);

    public async Task<JiraIssueDetails> GetIssueAsync(
        string issueKey,
        IReadOnlyList<string>? fields,
        CancellationToken cancellationToken)
    {
        var fieldsQuery = fields is { Count: > 0 } ? string.Join(",", fields) : "*all";

        var issue = await _api.GetIssue(issueKey, fieldsQuery, cancellationToken);

        if (issue is null)
        {
            throw new AtlassianApiException(
                AtlassianSystem.Jira,
                null,
                "Jira returned an empty response for the issue.");
        }

        return JiraIssueMapper.ToDetails(issue);
    }
}
