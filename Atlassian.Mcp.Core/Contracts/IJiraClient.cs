using Atlassian.Mcp.Core.Models.Jira;

namespace Atlassian.Mcp.Core.Contracts;

/// <summary>
/// Прикладные операции чтения Jira.
/// </summary>
public interface IJiraClient
{
    /// <summary>
    /// Проверяет доступность Jira и валидность Personal Access Token.
    /// </summary>
    Task CheckAvailabilityAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает задачу по ключу. Без списка полей запрашиваются все поля,
    /// включая пользовательские.
    /// </summary>
    Task<JiraIssueDetails> GetIssueAsync(
        string issueKey,
        IReadOnlyList<string>? fields,
        CancellationToken cancellationToken);
}
