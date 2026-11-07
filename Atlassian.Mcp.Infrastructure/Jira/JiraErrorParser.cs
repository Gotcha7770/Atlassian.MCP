using System.Text.Json;
using Atlassian.Mcp.Infrastructure.Jira.Models;

namespace Atlassian.Mcp.Infrastructure.Jira;

/// <summary>
/// Извлекает безопасное описание ошибки из тела ответа Jira
/// (<c>errorMessages</c>, <c>errors</c>).
/// </summary>
public static class JiraErrorParser
{
    public static string? ExtractMessage(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        ErrorResponse? response;

        try
        {
            response = JsonSerializer.Deserialize<ErrorResponse>(body, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return null;
        }

        if (response?.ErrorMessages is { Length: > 0 } messages)
        {
            return string.Join("; ", messages);
        }

        if (response?.Errors is { Count: > 0 } errors)
        {
            return string.Join("; ", errors.Select(error => $"{error.Key}: {error.Value}"));
        }

        return null;
    }
}

