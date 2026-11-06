using Atlassian.Mcp.Infrastructure;
using Atlassian.Mcp.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atlassian.Mcp.IntegrationTests;

/// <summary>
/// Общая инициализация HTTP-клиентов для интеграционных тестов: те же обработчики,
/// что и в приложении, с подменяемым primary handler.
/// </summary>
public sealed class HttpClientTestFixture
{
    public ServiceProvider CreateProvider(
        AtlassianOptions options,
        HttpMessageHandler? jiraHandler = null,
        HttpMessageHandler? confluenceHandler = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<AtlassianOptions>>(Options.Create(options));
        services.AddAtlassianHttpClients(jiraHandler, confluenceHandler);

        return services.BuildServiceProvider();
    }
}
