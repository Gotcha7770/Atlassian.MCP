using Atlassian.Mcp.Core.Models.Jira;
using Atlassian.Mcp.Infrastructure.Jira.Models;

namespace Atlassian.Mcp.Infrastructure.Jira;

/// <summary>
/// Преобразует страницу поиска Jira в модель MCP-инструмента.
/// </summary>
public static class IssueSearchMapper
{
    public static IssueSearchResponse ToResponse(IssueSearchPage page)
    {
        var issues = page.Issues
            .Select(issue => new IssueSearchItem
            {
                Key = issue.Key,
                Id = issue.Id,
                Fields = issue.Fields.ToDictionary(
                    field => field.Key,
                    field => field.Value.Clone(),
                    StringComparer.Ordinal)
            })
            .ToArray();

        int returnedCount = issues.Length;
        long nextStartAtValue = (long)page.StartAt + returnedCount;
        bool hasMore = nextStartAtValue < page.Total;
        int? nextStartAt = hasMore ? (int)nextStartAtValue : null;

        return new IssueSearchResponse
        {
            StartAt = page.StartAt,
            MaxResults = page.MaxResults,
            Total = page.Total,
            ReturnedCount = returnedCount,
            HasMore = hasMore,
            NextStartAt = nextStartAt,
            Issues = issues
        };
    }
}