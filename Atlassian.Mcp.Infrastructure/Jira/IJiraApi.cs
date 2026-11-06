using Atlassian.Mcp.Infrastructure.Jira.Models;
using Refit;

namespace Atlassian.Mcp.Infrastructure.Jira;

/// <summary>
/// REST API 2 Jira. Пути задаются абсолютными (требование Refit 16);
/// context path добавляет <see cref="Http.ContextPathHandler"/>.
/// </summary>
public interface IJiraApi
{
    [Get("/rest/api/2/myself")]
    Task<JiraCurrentUser> GetCurrentUser(CancellationToken cancellationToken);

    [Get("/rest/api/2/issue/{issueKey}")]
    Task<JiraIssue> GetIssue(
        string issueKey,
        [Query] string? fields,
        CancellationToken cancellationToken);
}
