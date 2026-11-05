using System.Text;
using Atlassian.Mcp.Core.Models.Common;

namespace Atlassian.Mcp.Core.Common;

/// <summary>
/// Ограничения объёма ответа инструментов MCP.
/// </summary>
public static class ResponseLimiter
{
    public const int MaxResponseBytes = 64 * 1024;

    public const int MaxTextLength = 16 * 1024;

    /// <summary>
    /// Усекает длинный текст до <see cref="MaxTextLength"/> символов.
    /// </summary>
    public static string? TruncateText(string? value, string fieldName, out TruncationInfo? truncation)
    {
        truncation = null;

        if (value is null || value.Length <= MaxTextLength)
        {
            return value;
        }

        truncation = new TruncationInfo
        {
            Field = fieldName,
            OriginalLength = value.Length,
            AppliedLength = MaxTextLength
        };

        return value[..MaxTextLength];
    }

    public static int GetUtf8Length(string json) => Encoding.UTF8.GetByteCount(json);

    public static bool IsWithinLimit(string json) => GetUtf8Length(json) <= MaxResponseBytes;
}
