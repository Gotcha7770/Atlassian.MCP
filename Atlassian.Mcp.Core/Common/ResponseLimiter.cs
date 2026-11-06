using System.Text;

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
    /// Суммарный бюджет пользовательских полей Jira в байтах UTF-8.
    /// </summary>
    public const int MaxCustomFieldsBytes = 32 * 1024;

    /// <summary>
    /// Общий предел элементов для связей, подзадач и пользовательских полей в ответе.
    /// Элементы распределяются по приоритету: `links` → `subtasks` → `customFields`.
    /// </summary>
    public const int MaxItems = 100;

    public static int GetUtf8Length(string value) => Encoding.UTF8.GetByteCount(value);
}
