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
    /// Максимальное количество пользовательских полей в ответе.
    /// </summary>
    public const int MaxCustomFields = 100;

    public static int GetUtf8Length(string value) => Encoding.UTF8.GetByteCount(value);
}
