using System.ComponentModel.DataAnnotations;

namespace Atlassian.Mcp.Infrastructure.Configuration;

/// <summary>
/// Настройка содержит поддерживаемый язык сообщений.
/// </summary>
public sealed class SupportedLanguageAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string text
            && AtlassianOptions.SupportedLanguages.Contains(text, StringComparer.OrdinalIgnoreCase))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessages.Format(ErrorMessages.UnsupportedLanguage, value));
    }
}

