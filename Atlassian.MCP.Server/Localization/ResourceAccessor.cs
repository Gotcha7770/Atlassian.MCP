using System.Globalization;
using System.Resources;

namespace Atlassian.MCP.Server.Localization;

/// <summary>
/// Доступ к локализованным строкам, видимым агенту.
/// </summary>
public sealed class ResourceAccessor
{
    private static readonly ResourceManager ToolDescriptions = new(
        "Atlassian.MCP.Server.Resources.ToolDescriptions",
        typeof(ResourceAccessor).Assembly);

    public ResourceAccessor(string language)
    {
        Culture = CultureInfo.GetCultureInfo(language);
    }

    public CultureInfo Culture { get; }

    public string ToolDescription(string toolName) => ToolDescriptions.GetString(toolName, Culture) ?? string.Empty;
}