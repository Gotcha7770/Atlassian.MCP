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
}
