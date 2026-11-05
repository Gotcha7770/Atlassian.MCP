namespace Atlassian.Mcp.Core.Contracts;

/// <summary>
/// Прикладные операции чтения Confluence.
/// </summary>
public interface IConfluenceClient
{
    /// <summary>
    /// Проверяет доступность Confluence и валидность Personal Access Token.
    /// </summary>
    Task CheckAvailabilityAsync(CancellationToken cancellationToken);
}
