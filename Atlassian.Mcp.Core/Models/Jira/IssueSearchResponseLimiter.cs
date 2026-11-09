using System.Text.Json;
using Atlassian.Mcp.Core.Common;

namespace Atlassian.Mcp.Core.Models.Jira;

/// <summary>
/// Ограничивает отдельные поля Jira Issue, не меняя состав страницы пагинации.
/// </summary>
public static class IssueSearchResponseLimiter
{
    public static IssueSearchResponse Limit(IssueSearchResponse response)
    {
        var issues = response.Issues
            .Select(issue =>
            {
                var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                var truncatedFields = new List<string>(issue.TruncatedFields);

                foreach (KeyValuePair<string, JsonElement> field in issue.Fields)
                {
                    if (fields.Count == ResponseLimiter.MaxItems)
                    {
                        truncatedFields.Add(field.Key);
                        continue;
                    }

                    JsonElement value = field.Value;

                    if (value.ValueKind == JsonValueKind.String)
                    {
                        string originalValue = value.GetString() ?? string.Empty;
                        string limitedValue = originalValue.TruncateToBytes(ResponseLimiter.MaxTextBytes);

                        if (limitedValue.Length < originalValue.Length)
                        {
                            truncatedFields.Add(field.Key);
                        }

                        string json = JsonSerializer.Serialize(limitedValue);
                        using var document = JsonDocument.Parse(json);
                        fields[field.Key] = document.RootElement.Clone();
                        continue;
                    }

                    string rawValue = value.GetRawText();
                    if (rawValue.Utf8Length > ResponseLimiter.MaxCustomFieldBytes)
                    {
                        truncatedFields.Add(field.Key);
                        continue;
                    }

                    fields[field.Key] = value.Clone();
                }

                return new IssueSearchItem
                {
                    Key = issue.Key,
                    Id = issue.Id,
                    Fields = fields,
                    TruncatedFields = truncatedFields
                };
            })
            .ToArray();

        return new IssueSearchResponse
        {
            StartAt = response.StartAt,
            MaxResults = response.MaxResults,
            Total = response.Total,
            ReturnedCount = response.ReturnedCount,
            HasMore = response.HasMore,
            NextStartAt = response.NextStartAt,
            Issues = issues
        };
    }
}