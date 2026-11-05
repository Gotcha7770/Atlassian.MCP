using System.Globalization;
using System.Resources;

namespace Atlassian.Mcp.Infrastructure.Configuration;

/// <summary>
/// Сообщения об ошибках приложения: валидация настроек и ошибки запуска.
/// Свойства используются DataAnnotations через <c>ErrorMessageResourceName</c>,
/// поэтому они статические и открытые.
/// </summary>
public static class ErrorMessages
{
    private static readonly ResourceManager Resources = new(
        "Atlassian.Mcp.Infrastructure.Resources.ErrorMessages",
        typeof(ErrorMessages).Assembly);

    public static string RequiredSetting => Get("RequiredSetting");

    public static string InvalidUrl => Get("InvalidUrl");

    public static string UnsupportedLanguage => Get("UnsupportedLanguage");

    public static string Format(string template, params object?[] arguments)
    {
        return string.Format(CultureInfo.CurrentUICulture, template, arguments);
    }

    private static string Get(string key) => Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}