using System.Text.Json;
using Atlassian.Mcp.Core.Common;
using Atlassian.Mcp.Core.Models.Common;

namespace Atlassian.Mcp.Core.Models.Jira;

/// <summary>
/// Приводит ответ <c>jiraGetIssue</c> к бюджетам объёма: длинное описание усекается,
/// пользовательские поля отбрасываются по суммарному размеру и количеству.
/// <c>truncation</c> описывает первое усечённое поле.
/// </summary>
public static class JiraIssueDetailsLimiter
{
    public static JiraIssueDetails Limit(JiraIssueDetails issue)
    {
        var description = LimitDescription(issue.Description);
        var links = LimitCollection(issue.Links, "links", ResponseLimiter.MaxItems);
        var usedByLinks = links.Value?.Count ?? 0;
        var subtasks = LimitCollection(issue.Subtasks, "subtasks", ResponseLimiter.MaxItems - usedByLinks);
        var usedByCollections = usedByLinks + (subtasks.Value?.Count ?? 0);
        var customFields = LimitCustomFields(
            issue.CustomFields,
            ResponseLimiter.MaxItems - usedByCollections);

        var truncation = description.Truncation
            ?? links.Truncation
            ?? subtasks.Truncation
            ?? customFields.Truncation;

        if (truncation is null)
        {
            return issue;
        }

        return issue with
        {
            Description = description.Value,
            CustomFields = customFields.Value,
            Links = links.Value,
            Subtasks = subtasks.Value,
            Truncated = true,
            Truncation = truncation
        };
    }

    private static Limited<string?> LimitDescription(string? description)
    {
        if (description is null)
        {
            return new Limited<string?>(null, null);
        }

        var shortened = description.TruncateToBytes(ResponseLimiter.MaxTextBytes);

        if (shortened.Length == description.Length)
        {
            return new Limited<string?>(description, null);
        }

        var truncation = new TruncationInfo
        {
            Field = "description",
            OriginalLength = description.Length,
            AppliedLength = shortened.Length
        };

        return new Limited<string?>(shortened, truncation);
    }

    /// <summary>
    /// Оставляет поля, пока их суммарный размер в байтах UTF-8 не превысил бюджет
    /// и количество не достигло предела. Поле, которое не влезает, пропускается:
    /// одно большое поле не отбрасывает следующие за ним.
    /// </summary>
    private static Limited<Dictionary<string, JsonElement>?> LimitCustomFields(
        Dictionary<string, JsonElement>? customFields,
        int maxItems)
    {
        if (customFields is not { Count: > 0 })
        {
            return new Limited<Dictionary<string, JsonElement>?>(customFields, null);
        }

        var kept = new Dictionary<string, JsonElement>(customFields.Count);
        var bytes = 0;

        foreach (var field in customFields)
        {
            if (kept.Count == maxItems)
            {
                break;
            }

            var size = ResponseLimiter.GetUtf8Length(field.Value.GetRawText());

            if (bytes + size > ResponseLimiter.MaxCustomFieldsBytes)
            {
                continue;
            }

            kept[field.Key] = field.Value;
            bytes += size;
        }

        if (kept.Count == customFields.Count)
        {
            return new Limited<Dictionary<string, JsonElement>?>(customFields, null);
        }

        var truncation = new TruncationInfo
        {
            Field = "customFields",
            OriginalLength = customFields.Count,
            AppliedLength = kept.Count
        };

        return new Limited<Dictionary<string, JsonElement>?>(kept.Count == 0 ? null : kept, truncation);
    }

    /// <summary>
    /// Оставляет первые <paramref name="maxItems"/> элементов коллекции.
    /// </summary>
    private static Limited<IReadOnlyList<T>?> LimitCollection<T>(
        IReadOnlyList<T>? items,
        string field,
        int maxItems)
    {
        if (items is null || items.Count <= maxItems)
        {
            return new(items, null);
        }

        var truncation = new TruncationInfo
        {
            Field = field,
            OriginalLength = items.Count,
            AppliedLength = Math.Max(maxItems, 0)
        };

        return new(maxItems <= 0 ? null : items.Take(maxItems).ToList(), truncation);
    }

    /// <summary>
    /// Значение после применения бюджета и описание усечения, если оно произошло.
    /// </summary>
    private sealed record Limited<T>(T Value, TruncationInfo? Truncation);
}
