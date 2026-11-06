using System.Net;
using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Core.Errors;
using Atlassian.Mcp.Infrastructure.Configuration;
using Atlassian.Mcp.Infrastructure.Confluence;
using Atlassian.Mcp.Infrastructure.Http;
using Atlassian.Mcp.Infrastructure.Jira;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace Atlassian.Mcp.Infrastructure;

/// <summary>
/// Регистрация настроек и HTTP-клиентов Jira и Confluence.
/// </summary>
public static class DependencyInjection
{
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(30);

    private static readonly HttpRetryOptions RetryOptions = new()
    {
        MaxRetries = 1,
        BaseDelay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromSeconds(10),
        UseJitter = false,
        RetryHttpRequestExceptions = false,
        ShouldRetry = static response => response.StatusCode == HttpStatusCode.TooManyRequests,
        DelayProvider = RetryDelayProviders.Jira
    };

    // Refit 16 по умолчанию оборачивает исключения обработчиков в ApiRequestException;
    // AtlassianApiException из ErrorHandler должен выходить наружу без обёртки.
    private static readonly RefitSettings Settings = new()
    {
        TransportExceptionFactory = (_, exception, _) => exception
    };

    public static IServiceCollection AddAtlassian(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<AtlassianOptions>()
            .Bind(configuration.GetSection(AtlassianOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services.AddAtlassianHttpClients();
    }

    public static IServiceCollection AddAtlassianHttpClients(
        this IServiceCollection services,
        HttpMessageHandler? jiraPrimaryHandler = null,
        HttpMessageHandler? confluencePrimaryHandler = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var jiraClient = services.AddRefitClient<IJiraApi>(Settings)
            .ConfigureHttpClient((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<AtlassianOptions>>();
                client.BaseAddress = options.Value.JiraUrl.GetBaseAddress();
                client.Timeout = HttpTimeout;
            })
            .AddHttpMessageHandler(_ => new ErrorHandler(AtlassianSystem.Jira, JiraErrorParser.ExtractMessage))
            .AddHttpMessageHandler(provider =>
            {
                var options = provider.GetRequiredService<IOptions<AtlassianOptions>>();
                return new BearerTokenHandler(options.Value.JiraPat);
            })
            .AddHttpMessageHandler(_ => new HttpRetryHandler(RetryOptions))
            .AddHttpMessageHandler(provider =>
            {
                var options = provider.GetRequiredService<IOptions<AtlassianOptions>>();
                var prefix = options.Value.JiraUrl.GetPathPrefix();

                return new ContextPathHandler(prefix);
            });

        if (jiraPrimaryHandler is not null)
        {
            jiraClient.ConfigurePrimaryHttpMessageHandler(() => jiraPrimaryHandler);
        }

        var confluenceClient = services.AddRefitClient<IConfluenceApi>(Settings)
            .ConfigureHttpClient((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<AtlassianOptions>>();
                client.BaseAddress = options.Value.ConfluenceUrl.GetBaseAddress();
                client.Timeout = HttpTimeout;
            })
            .AddHttpMessageHandler(_ => new ErrorHandler(
                AtlassianSystem.Confluence,
                ConfluenceErrorParser.ExtractMessage))
            .AddHttpMessageHandler(provider =>
            {
                var options = provider.GetRequiredService<IOptions<AtlassianOptions>>();
                return new BearerTokenHandler(options.Value.ConfluencePat);
            })
            .AddHttpMessageHandler(_ => new HttpRetryHandler(RetryOptions))
            .AddHttpMessageHandler(provider =>
            {
                var options = provider.GetRequiredService<IOptions<AtlassianOptions>>();
                var prefix = options.Value.ConfluenceUrl.GetPathPrefix();

                return new ContextPathHandler(prefix);
            });

        if (confluencePrimaryHandler is not null)
        {
            confluenceClient.ConfigurePrimaryHttpMessageHandler(() => confluencePrimaryHandler);
        }

        services.AddSingleton<IJiraClient, JiraClient>();
        services.AddSingleton<IConfluenceClient, ConfluenceClient>();

        return services;
    }
}

