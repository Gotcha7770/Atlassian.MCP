using System.Text.Json;
using Atlassian.Mcp.Infrastructure.Confluence.Models;

namespace Atlassian.Mcp.Infrastructure.Confluence;

/// <summary>
/// Извлекает безопасное описание ошибки из тела ответа Confluence (<c>message</c>).
/// </summary>
public static class ConfluenceErrorParser
{
    public static string? ExtractMessage(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            var response = JsonSerializer.Deserialize<ErrorResponse>(body, JsonSerializerOptions.Web);

            return response?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

