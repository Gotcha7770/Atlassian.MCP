namespace Atlassian.Mcp.Core.Common;

/// <summary>
/// Бюджеты объёма ответа инструментов MCP. Ответ ограничивается не измерением
/// сериализованного объекта, а бюджетами отдельных частей.
/// </summary>
public static class ResponseLimiter
{
    /// <summary>
    /// Бюджет текстового значения (описание задачи, тело страницы) в байтах UTF-8.
    /// </summary>
    public const int MaxTextBytes = 16 * 1024;

    /// <summary>
    /// Максимальный размер значения одного пользовательского поля Jira в байтах UTF-8.
    /// </summary>
    public const int MaxCustomFieldBytes = 512;

    /// <summary>
    /// Общий предел элементов для связей, подзадач и пользовательских полей в ответе.
    /// </summary>
    public const int MaxItems = 100;
}
