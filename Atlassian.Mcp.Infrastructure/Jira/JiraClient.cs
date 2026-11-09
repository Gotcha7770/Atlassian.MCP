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

    public async Task CheckAvailabilityAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "{System}: {Operation} started",
            AtlassianSystem.Jira,
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
                AtlassianSystem.Jira,
                nameof(CheckAvailabilityAsync),
                exception.StatusCode is { } status ? (int)status : null);

            throw;
        }

        _logger.LogInformation(
            "{System}: {Operation} completed",
            AtlassianSystem.Jira,
            nameof(CheckAvailabilityAsync));
    }

    public async Task<IssueResponse> GetIssueAsync(
        string issueKey,
        IReadOnlyList<string>? fields,
        CancellationToken cancellationToken)
    {
        var fieldsQuery = fields is { Count: > 0 } ? string.Join(",", fields) : "*all";

        _logger.LogDebug(
            "{System}: {Operation} started",
            AtlassianSystem.Jira,
            nameof(GetIssueAsync));

        Issue? issue;

        try
        {
            issue = await _api.GetIssue(issueKey, fieldsQuery, cancellationToken);
        }
        catch (AtlassianApiException exception)
        {
            _logger.LogError(
                exception,
                "{System}: {Operation} failed with {StatusCode}",
                AtlassianSystem.Jira,
                nameof(GetIssueAsync),
                exception.StatusCode is { } status ? (int)status : null);

            throw;
        }

        _logger.LogInformation(
            "{System}: {Operation} completed",
            AtlassianSystem.Jira,
            nameof(GetIssueAsync));

        if (issue is null)
        {
            throw new AtlassianApiException(
                AtlassianSystem.Jira,
                null,
                $"{AtlassianSystem.Jira}: {ErrorMessages.EmptyResponse}");
        }

        return IssueMapper.ToDetails(issue);
    }

    public async Task<IssueSearchResponse> SearchIssuesAsync(
        string jql,
        int startAt,
        int maxResults,
        IReadOnlyList<string>? fields,
        CancellationToken cancellationToken)
    {
        string fieldsQuery = fields is { Count: > 0 }
            ? string.Join(",", fields)
            : "summary,status,issuetype,project";

        _logger.LogDebug(
            "{System}: {Operation} started",
            AtlassianSystem.Jira,
            nameof(SearchIssuesAsync));

        ApiResponse<IssueSearchPage> response;

        try
        {
            response = await _api.SearchIssues(jql, startAt, maxResults, fieldsQuery, cancellationToken);
        }
        catch (AtlassianApiException exception)
        {
            _logger.LogError(
                exception,
                "{System}: {Operation} failed with {StatusCode}",
                AtlassianSystem.Jira,
                nameof(SearchIssuesAsync),
                exception.StatusCode is { } status ? (int)status : null);

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

            _logger.LogInformation(
                "{System}: {Operation} completed",
                AtlassianSystem.Jira,
                nameof(SearchIssuesAsync));

            return mappedResponse;
        }
    }
}