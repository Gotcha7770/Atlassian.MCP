using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace Atlassian.Mcp.Infrastructure.Configuration;

/// <summary>
/// Настройки подключения к Jira и Confluence, задаваемые при запуске процесса.
/// </summary>
public sealed class AtlassianOptions
{
    public const string SectionName = "Atlassian";

    public const string DefaultLanguage = "en";

    public static readonly string[] SupportedLanguages = ["en", "ru"];

    public const string JiraUrlKey = "ATLASSIAN_JIRA_URL";
    public const string JiraPatKey = "ATLASSIAN_JIRA_PAT";
    public const string ConfluenceUrlKey = "ATLASSIAN_CONFLUENCE_URL";
    public const string ConfluencePatKey = "ATLASSIAN_CONFLUENCE_PAT";
    public const string LanguageKey = "ATLASSIAN_MCP_LANGUAGE";

    [Required(
        ErrorMessageResourceType = typeof(ErrorMessages),
        ErrorMessageResourceName = nameof(ErrorMessages.RequiredSetting))]
    [AbsoluteHttpUrl]
    [Display(Name = JiraUrlKey)]
    public required Uri JiraUrl { get; init; }

    [Required(
        ErrorMessageResourceType = typeof(ErrorMessages),
        ErrorMessageResourceName = nameof(ErrorMessages.RequiredSetting))]
    [Display(Name = JiraPatKey)]
    public required string JiraPat { get; init; }

    [Required(
        ErrorMessageResourceType = typeof(ErrorMessages),
        ErrorMessageResourceName = nameof(ErrorMessages.RequiredSetting))]
    [AbsoluteHttpUrl]
    [Display(Name = ConfluenceUrlKey)]
    public required Uri ConfluenceUrl { get; init; }

    [Required(
        ErrorMessageResourceType = typeof(ErrorMessages),
        ErrorMessageResourceName = nameof(ErrorMessages.RequiredSetting))]
    [Display(Name = ConfluencePatKey)]
    public required string ConfluencePat { get; init; }

    [SupportedLanguage]
    [Display(Name = LanguageKey)]
    public string Language { get; init; } = DefaultLanguage;

    /// <summary>
    /// Раскладывает переменные окружения по секции <see cref="SectionName"/>,
    /// чтобы связать их с типизированными настройками через <c>Bind</c>.
    /// </summary>
    public static IReadOnlyDictionary<string, string?> ToSectionValues(IConfiguration configuration)
    {
        return new Dictionary<string, string?>
        {
            [$"{SectionName}:{nameof(JiraUrl)}"] = configuration[JiraUrlKey],
            [$"{SectionName}:{nameof(JiraPat)}"] = configuration[JiraPatKey],
            [$"{SectionName}:{nameof(ConfluenceUrl)}"] = configuration[ConfluenceUrlKey],
            [$"{SectionName}:{nameof(ConfluencePat)}"] = configuration[ConfluencePatKey],
            [$"{SectionName}:{nameof(Language)}"] = configuration[LanguageKey] is { Length: > 0 } language
                ? language
                : DefaultLanguage
        };
    }

    /// <summary>
    /// Язык сообщений до валидации настроек: поддерживаемое значение или язык по умолчанию.
    /// </summary>
    public static string ResolveLanguage(string? configuredLanguage)
    {
        return SupportedLanguages.Contains(configuredLanguage, StringComparer.OrdinalIgnoreCase)
            ? configuredLanguage!
            : DefaultLanguage;
    }
}
