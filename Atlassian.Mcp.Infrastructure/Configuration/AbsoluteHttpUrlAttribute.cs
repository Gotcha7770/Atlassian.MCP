using System.ComponentModel.DataAnnotations;

namespace Atlassian.Mcp.Infrastructure.Configuration;

/// <summary>
/// Настройка содержит абсолютный URL со схемой http или https без query и fragment.
/// </summary>
public sealed class AbsoluteHttpUrlAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not Uri uri)
        {
            return ValidationResult.Success;
        }

        var isValid = uri.IsAbsoluteUri
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment);

        return isValid
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessages.Format(ErrorMessages.InvalidUrl, validationContext.DisplayName));
    }
}
