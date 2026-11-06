using System.Text.Json;
using Atlassian.Mcp.Core.Models.Jira;
using Atlassian.Mcp.Infrastructure.Jira.Models;
using CoreIssueLink = Atlassian.Mcp.Core.Models.Jira.JiraIssueLink;
using CoreSubtask = Atlassian.Mcp.Core.Models.Jira.JiraSubtask;
using InfraIssueLink = Atlassian.Mcp.Infrastructure.Jira.Models.JiraIssueLink;
using InfraSubtask = Atlassian.Mcp.Infrastructure.Jira.Models.JiraSubtask;

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
            Subtasks = MapSubtasks(fields?.Subtasks),
            Links = MapBlockingLinks(fields?.IssueLinks),
            CustomFields = ExtractCustomFields(fields?.AdditionalFields)
        };
    }

    private static CoreSubtask[]? MapSubtasks(InfraSubtask[]? subtasks)
    {
        if (subtasks is null)
        {
            return null;
        }

        var mapped = subtasks
            .Where(subtask => !string.IsNullOrWhiteSpace(subtask.Key))
            .Select(subtask => new CoreSubtask
            {
                Key = subtask.Key!,
                Summary = subtask.Fields?.Summary,
                Status = subtask.Fields?.Status?.Name
            })
            .ToArray();

        return mapped.Length == 0 ? null : mapped;
    }

    private static CoreIssueLink[]? MapBlockingLinks(InfraIssueLink[]? links)
    {
        if (links is null)
        {
            return null;
        }

        var mapped = new List<CoreIssueLink>();

        foreach (var link in links)
        {
            if (!IsBlocking(link.Type))
            {
                continue;
            }

            var (direction, issue) = link.OutwardIssue is not null
                ? ("outward", link.OutwardIssue)
                : ("inward", link.InwardIssue);

            if (issue?.Key is null)
            {
                continue;
            }

            mapped.Add(new CoreIssueLink
            {
                Type = link.Type!.Name!,
                Direction = direction,
                Key = issue.Key,
                Summary = issue.Fields?.Summary,
                Status = issue.Fields?.Status?.Name
            });
        }

        return mapped.Count == 0 ? null : mapped.ToArray();
    }

    private static bool IsBlocking(JiraLinkType? type) =>
        type?.Name is { } name
        && (name.Contains("block", StringComparison.OrdinalIgnoreCase)
            || name.Contains("блок", StringComparison.OrdinalIgnoreCase));

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
