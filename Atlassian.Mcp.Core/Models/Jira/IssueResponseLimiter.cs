using System.Text.Json;
using Atlassian.Mcp.Core.Common;

namespace Atlassian.Mcp.Core.Models.Jira;

/// <summary>
/// Приводит ответ <c>jiraGetIssue</c> к бюджетам объёма: длинное описание усекается,
/// пользовательские поля отбрасываются по суммарному размеру и количеству.
/// <c>truncatedProperties</c> перечисляет все усечённые поля.
/// </summary>
public static class IssueResponseLimiter
{
    public static IssueResponse Limit(IssueResponse issue)
    {
        var (description, truncatedDescription) = LimitDescription(issue.Description);
        var (links, truncatedLinks) = LimitCollection(issue.Links, "links", ResponseLimiter.MaxItems);
        var usedByLinks = links?.Count ?? 0;
        var (subtasks, truncatedSubtasks) = LimitCollection(
            issue.Subtasks,
            "subtasks",
            ResponseLimiter.MaxItems - usedByLinks);
        var usedByCollections = usedByLinks + (subtasks?.Count ?? 0);
        var (customFields, truncatedCustomFields) = LimitCustomFields(
            issue.CustomFields,
            ResponseLimiter.MaxItems - usedByCollections);

        var truncatedProperties = new[]
            {
                truncatedDescription,
                truncatedLinks,
                truncatedSubtasks,
                truncatedCustomFields
            }
            .OfType<string>()
            .ToArray();

        return new IssueResponse
        {
            Key = issue.Key,
            Id = issue.Id,
            Summary = issue.Summary,
            Description = description,
            Status = issue.Status,
            IssueType = issue.IssueType,
            ProjectKey = issue.ProjectKey,
            Assignee = issue.Assignee,
            Reporter = issue.Reporter,
            Priority = issue.Priority,
            Created = issue.Created,
            Updated = issue.Updated,
            Labels = issue.Labels,
            Subtasks = subtasks,
            Links = links,
            CustomFields = customFields,
            TruncatedProperties = truncatedProperties
        };
    }

    private static (string? Value, string? TruncatedProperty) LimitDescription(string? description)
    {
        if (description is null)
        {
            return (null, null);
        }

        var shortened = description.TruncateToBytes(ResponseLimiter.MaxTextBytes);

        if (shortened.Length == description.Length)
        {
            return (description, null);
        }

        return (shortened, "description");
    }

    /// <summary>
    /// Оставляет поля, пока их количество не достигло предела. Поле, значение которого
    /// больше допустимого размера, пропускается: одно большое поле не отбрасывает
    /// следующие за ним.
    /// </summary>
    private static (Dictionary<string, JsonElement>? Value, string? TruncatedProperty) LimitCustomFields(
        Dictionary<string, JsonElement>? customFields,
        int maxItems)
    {
        if (customFields is not { Count: > 0 })
        {
            return (customFields, null);
        }

        if (maxItems <= 0)
        {
            return (null, "customFields");
        }

        var kept = new Dictionary<string, JsonElement>(customFields.Count);

        foreach (var field in customFields)
        {
            if (kept.Count == maxItems)
            {
                break;
            }

            var size = ResponseLimiter.GetUtf8Length(field.Value.GetRawText());

            if (size > ResponseLimiter.MaxCustomFieldBytes)
            {
                continue;
            }

            kept[field.Key] = field.Value;
        }

        if (kept.Count == customFields.Count)
        {
            return (customFields, null);
        }

        return (kept.Count == 0 ? null : kept, "customFields");
    }

    /// <summary>
    /// Оставляет первые <paramref name="maxItems"/> элементов коллекции.
    /// </summary>
    private static (IReadOnlyList<T>? Value, string? TruncatedProperty) LimitCollection<T>(
        IReadOnlyList<T>? items,
        string field,
        int maxItems)
    {
        if (items is null || items.Count <= maxItems)
        {
            return (items, null);
        }

        return (maxItems <= 0 ? null : items.Take(maxItems).ToList(), field);
    }
}
