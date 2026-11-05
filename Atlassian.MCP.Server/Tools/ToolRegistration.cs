using System.Reflection;
using Atlassian.MCP.Server.Localization;
using ModelContextProtocol.Server;

namespace Atlassian.MCP.Server.Tools;

/// <summary>
/// Создаёт инструменты MCP с локализованными описаниями.
/// Атрибутные описания остаются английским fallback-ом, ресурсы их перекрывают.
/// </summary>
public static class ToolRegistration
{
    public static IReadOnlyList<McpServerTool> Create(
        IEnumerable<Type> toolTypes,
        Func<Type, object> resolve,
        ResourceAccessor resources)
    {
        ArgumentNullException.ThrowIfNull(toolTypes);
        ArgumentNullException.ThrowIfNull(resolve);
        ArgumentNullException.ThrowIfNull(resources);

        var tools = new List<McpServerTool>();

        foreach (var toolType in toolTypes)
        {
            var instance = resolve(toolType);

            foreach (var method in toolType.GetMethods(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var attribute = method.GetCustomAttribute<McpServerToolAttribute>();
                if (attribute is null)
                {
                    continue;
                }

                var name = string.IsNullOrWhiteSpace(attribute.Name) ? method.Name : attribute.Name!;

                tools.Add(McpServerTool.Create(method, instance, new McpServerToolCreateOptions
                {
                    Name = name,
                    Description = resources.ToolDescription(name)
                }));
            }
        }

        return tools;
    }
}
