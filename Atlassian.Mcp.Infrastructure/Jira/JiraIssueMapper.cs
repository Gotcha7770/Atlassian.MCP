using Atlassian.Mcp.Core.Models.Jira;
using Atlassian.Mcp.Infrastructure.Jira.Models;

namespace Atlassian.Mcp.Infrastructure.Jira;

/// <summary>
/// Преобразует задачу REST API Jira в модель MCP-инструмента.
/// </summary>
public static class JiraIssueMapper
{
    public static JiraIssueDetails ToDetails(JiraIssue issue)
    {
        var fields = issue.Fields;

        return new JiraIssueDetails
        {
            Key = issue.Key ?? string.Empty,
            Id = issue.Id,
            Summary = fields?.Summary,
            Description = fields?.Description,
            Status = fields?.Status?.Name,
            IssueType = fields?.IssueType?.Name,
            ProjectKey = fields?.Project?.Key,
            Assignee = fields?.Assignee?.DisplayName,
            Reporter = fields?.Reporter?.DisplayName,
            Priority = fields?.Priority?.Name,
            Created = fields?.Created,
            Updated = fields?.Updated,
            Labels = fields?.Labels,
            CustomFields = fields?.AdditionalFields
        };
    }
}
