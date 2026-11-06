using System.Text.Json;
using Atlassian.Mcp.Core.Models.Jira;
using Atlassian.Mcp.Infrastructure.Jira.Models;

namespace Atlassian.Mcp.Infrastructure.Jira;

/// <summary>
/// Преобразует задачу REST API Jira в модель MCP-инструмента.
/// </summary>
public static class JiraIssueMapper
{
    private const string CustomFieldPrefix = "customfield_";

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
            CustomFields = ExtractCustomFields(fields?.AdditionalFields)
        };
    }

    private static Dictionary<string, JsonElement>? ExtractCustomFields(
        Dictionary<string, JsonElement>? additionalFields)
    {
        if (additionalFields is null)
        {
            return null;
        }

        var customFields = additionalFields
            .Where(field => field.Key.StartsWith(CustomFieldPrefix, StringComparison.Ordinal))
            .Where(field => field.Value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            .ToDictionary(field => field.Key, field => field.Value);

        return customFields.Count == 0 ? null : customFields;
    }
}
