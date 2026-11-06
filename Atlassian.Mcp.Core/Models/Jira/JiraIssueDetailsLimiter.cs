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
        var customFields = LimitCustomFields(issue.CustomFields);
        var truncation = description.Truncation ?? customFields.Truncation;

        if (truncation is null)
        {
            return issue;
        }

        return issue with
        {
            Description = description.Value,
            CustomFields = customFields.Value,
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
        Dictionary<string, JsonElement>? customFields)
    {
        if (customFields is not { Count: > 0 })
        {
            return new Limited<Dictionary<string, JsonElement>?>(customFields, null);
        }

        var kept = new Dictionary<string, JsonElement>(customFields.Count);
        var bytes = 0;

        foreach (var field in customFields)
        {
            if (kept.Count == ResponseLimiter.MaxCustomFields)
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
    /// Значение после применения бюджета и описание усечения, если оно произошло.
    /// </summary>
    private sealed record Limited<T>(T Value, TruncationInfo? Truncation);
}
