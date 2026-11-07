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
        var truncatedProperties = new List<string>();
        int itemsBudget = ResponseLimiter.MaxItems;

        var description = issue.Description?.TruncateToBytes(ResponseLimiter.MaxTextBytes);
        if(issue.Description?.Length > description?.Length)
            truncatedProperties.Add("description");

        var links = issue.Links?.Take(itemsBudget).ToArray();
        itemsBudget -= links?.Length ?? 0;
        if(issue.Links?.Count > links?.Length)
            truncatedProperties.Add("links");

        var subtasks = issue.Subtasks?.Take(itemsBudget).ToArray();
        itemsBudget -= subtasks?.Length ?? 0;
        if(issue.Subtasks?.Count > subtasks?.Length)
            truncatedProperties.Add("subtasks");

        var customFields = issue.CustomFields?
            .Where(x => x.Value.GetRawText().Utf8Length < ResponseLimiter.MaxCustomFieldBytes)
            .Take(itemsBudget)
            .ToDictionary();
        if(issue.CustomFields?.Count > customFields?.Count)
            truncatedProperties.Add("customFields");

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
            CustomFields = customFields is { Count: > 0} ? customFields : null,
            TruncatedProperties = truncatedProperties
        };
    }
}
