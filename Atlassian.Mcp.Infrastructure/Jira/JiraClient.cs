using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Core.Errors;
using Atlassian.Mcp.Core.Models.Jira;
using Atlassian.Mcp.Infrastructure.Configuration;
using Atlassian.Mcp.Infrastructure.Jira.Models;
using Microsoft.Extensions.Logging;
using Refit;

namespace Atlassian.Mcp.Infrastructure.Jira;

/// <summary>
/// Прикладной клиент Jira.
/// </summary>
public sealed class JiraClient : IJiraClient
{
    private readonly IJiraApi _api;
    private readonly ILogger<JiraClient> _logger;

    public JiraClient(IJiraApi api, ILogger<JiraClient> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task CheckAvailability(CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope("{System}: {Operation}", AtlassianSystem.Jira, nameof(CheckAvailability));

        _logger.LogDebug("started");

        try
        {
            await _api.GetCurrentUser(cancellationToken);
        }
        catch (AtlassianApiException exception)
        {
            _logger.LogError(exception, "failed with {StatusCode}", exception.StatusCode);

            throw;
        }

        _logger.LogInformation("completed");
    }

    public async Task<IssueResponse> GetIssue(
        string issueKey,
        IReadOnlyList<string>? fields,
        CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope("{System}: {Operation}", AtlassianSystem.Jira, nameof(GetIssue));

        _logger.LogDebug("started");

        Issue? issue;
        var fieldsQuery = fields is { Count: > 0 } ? string.Join(",", fields) : "*all";

        try
        {
            issue = await _api.GetIssue(issueKey, fieldsQuery, cancellationToken);
        }
        catch (AtlassianApiException exception)
        {
            _logger.LogError(exception, "failed with {StatusCode}", exception.StatusCode);

            throw;
        }

        _logger.LogInformation("completed");

        if (issue is null)
        {
            throw new AtlassianApiException(
                AtlassianSystem.Jira,
                null,
                $"{AtlassianSystem.Jira}: {ErrorMessages.EmptyResponse}");
        }

        return IssueMapper.ToDetails(issue);
    }

    public async Task<IssueSearchResponse> SearchIssues(
        string jql,
        int startAt,
        int maxResults,
        IReadOnlyList<string>? fields,
        CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope("{System}: {Operation}", AtlassianSystem.Jira, nameof(SearchIssues));

        string fieldsQuery = fields is { Count: > 0 }
            ? string.Join(",", fields)
            : "summary,status,issuetype,project";

        _logger.LogDebug("started");

        ApiResponse<IssueSearchPage> response;

        try
        {
            response = await _api.SearchIssues(jql, startAt, maxResults, fieldsQuery, cancellationToken);
        }
        catch (AtlassianApiException exception)
        {
            _logger.LogError(exception, "failed with {StatusCode}", exception.StatusCode);

            throw;
        }

        using (response)
        {
            if (response.Content is null)
            {
                throw new AtlassianApiException(
                    AtlassianSystem.Jira,
                    response.StatusCode,
                    $"{AtlassianSystem.Jira}: {ErrorMessages.EmptyResponse}");
            }

            IssueSearchResponse mappedResponse = IssueSearchMapper.ToResponse(response.Content);

            _logger.LogInformation("completed");

            return mappedResponse;
        }
    }
}