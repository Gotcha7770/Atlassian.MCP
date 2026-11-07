using Atlassian.Mcp.Infrastructure.Confluence.Models;
using Refit;

namespace Atlassian.Mcp.Infrastructure.Confluence;

/// <summary>
/// REST API Confluence. Пути задаются абсолютными (требование Refit 16);
/// context path добавляет <see cref="Http.ContextPathHandler"/>.
/// </summary>
public interface IConfluenceApi
{
    [Get("/rest/api/user/current")]
    Task<CurrentUser> GetCurrentUser(CancellationToken cancellationToken);
}

