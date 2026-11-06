using System.Globalization;
using System.Resources;

namespace Atlassian.MCP.Server.Resources;

/// <summary>
/// Сообщения MCP-инструментов, видимые агенту.
/// </summary>
public static class ToolMessages
{
    private static readonly ResourceManager Resources = new(
        "Atlassian.MCP.Server.Resources.ToolMessages",
        typeof(ToolMessages).Assembly);

    public static string IssueKeyRequired => Get("IssueKeyRequired");

    private static string Get(string key) => Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
