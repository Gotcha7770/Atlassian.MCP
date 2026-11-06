using System.Globalization;
using Atlassian.Mcp.Core.Errors;
using Atlassian.Mcp.Infrastructure;
using Atlassian.Mcp.Infrastructure.Configuration;
using Atlassian.MCP.Server.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using Spectre.Console;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// stdout зарезервирован под MCP STDIO: любой вывод Spectre.Console идёт в stderr.
var console = AnsiConsole.Create(new AnsiConsoleSettings
{
    Ansi = AnsiSupport.No,
    ColorSystem = ColorSystemSupport.NoColors,
    Out = new AnsiConsoleOutput(Console.Error)
});

var culture = CultureInfo.GetCultureInfo(
    AtlassianOptions.ResolveLanguage(builder.Configuration[AtlassianOptions.LanguageKey]));
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

builder.Configuration.AddInMemoryCollection(AtlassianOptions.ToSectionValues(builder.Configuration));

builder.Services.AddSingleton(console);
builder.Services.AddAtlassian(builder.Configuration);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<JiraTools>()
    .WithRequestFilters(filters => filters.AddCallToolFilter(next =>
        async (context, cancellationToken) =>
        {
            try
            {
                return await next(context, cancellationToken);
            }
            catch (AtlassianApiException exception)
            {
                throw new McpException(exception.Message, exception);
            }
        }));

var app = builder.Build();

await app.RunAsync();