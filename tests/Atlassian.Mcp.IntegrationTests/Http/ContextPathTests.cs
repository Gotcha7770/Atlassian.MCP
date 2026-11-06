using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Infrastructure;
using Atlassian.Mcp.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace Atlassian.Mcp.IntegrationTests.Http;

public sealed class ContextPathTests
{
    [Fact]
    public async Task Jira_request_keeps_context_path()
    {
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://jira.example.com/jira/rest/api/2/myself")
            .Respond("application/json", """{"name":"tester"}""");

        await using var provider = BuildProvider(jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        await client.CheckAvailabilityAsync(TestContext.Current.CancellationToken);

        mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public async Task Confluence_request_keeps_context_path()
    {
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://confluence.example.com/wiki/rest/api/user/current")
            .Respond("application/json", """{"username":"tester"}""");

        await using var provider = BuildProvider(confluenceHandler: mock);
        var client = provider.GetRequiredService<IConfluenceClient>();

        await client.CheckAvailabilityAsync(TestContext.Current.CancellationToken);

        mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public async Task Each_system_uses_its_own_token()
    {
        var mock = new MockHttpMessageHandler();
        var jiraRequest = mock
            .When("https://jira.example.com/jira/rest/api/2/myself")
            .WithHeaders("Authorization", "Bearer jira-token")
            .Respond("application/json", """{"name":"tester"}""");
        var confluenceRequest = mock
            .When("https://confluence.example.com/wiki/rest/api/user/current")
            .WithHeaders("Authorization", "Bearer confluence-token")
            .Respond("application/json", """{"username":"tester"}""");

        await using var provider = BuildProvider(jiraHandler: mock, confluenceHandler: mock);

        await provider.GetRequiredService<IJiraClient>().CheckAvailabilityAsync(TestContext.Current.CancellationToken);
        await provider.GetRequiredService<IConfluenceClient>().CheckAvailabilityAsync(TestContext.Current.CancellationToken);

        mock.GetMatchCount(jiraRequest).Should().Be(1);
        mock.GetMatchCount(confluenceRequest).Should().Be(1);
    }

    private static ServiceProvider BuildProvider(
        HttpMessageHandler? jiraHandler = null,
        HttpMessageHandler? confluenceHandler = null)
    {
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri("https://jira.example.com/jira"),
            JiraPat = "jira-token",
            ConfluenceUrl = new Uri("https://confluence.example.com/wiki"),
            ConfluencePat = "confluence-token"
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<AtlassianOptions>>(Options.Create(options));
        services.AddAtlassianHttpClients(jiraHandler, confluenceHandler);

        return services.BuildServiceProvider();
    }
}


