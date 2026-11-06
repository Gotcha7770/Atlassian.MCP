using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atlassian.Mcp.Infrastructure.Jira.Models;

/// <summary>
/// Поля задачи Jira: типизированные стандартные и произвольные неизвестные.
/// </summary>
public sealed record JiraIssueFields
{
    public string? Summary { get; init; }

    public string? Description { get; init; }

    public JiraNamedValue? Status { get; init; }

    public JiraNamedValue? IssueType { get; init; }

    public JiraProjectRef? Project { get; init; }

    public JiraUser? Assignee { get; init; }

    public JiraUser? Reporter { get; init; }

    public JiraNamedValue? Priority { get; init; }

    public string? Created { get; init; }

    public string? Updated { get; init; }

    public string[]? Labels { get; init; }

    public JiraSubtask[]? Subtasks { get; init; }

    public JiraIssueLink[]? IssueLinks { get; init; }

    /// <summary>
    /// Все поля, не описанные типизированными свойствами: пользовательские
    /// (<c>customfield_*</c>) и неизвестные стандартные.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalFields { get; init; }
}
