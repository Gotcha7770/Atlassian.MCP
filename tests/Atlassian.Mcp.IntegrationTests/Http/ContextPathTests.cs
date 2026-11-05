using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Infrastructure;
using Atlassian.Mcp.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Atlassian.Mcp.IntegrationTests.Http;

public sealed class ContextPathTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    [Fact]
    public async Task Jira_request_keeps_context_path()
    {
        _server
            .Given(Request.Create().WithPath("/jira/rest/api/2/myself").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { name = "tester" }));

        await using var provider = BuildProvider(jiraPath: "/jira", confluencePath: "/wiki");

        await provider.GetRequiredService<IJiraClient>().CheckAvailabilityAsync(TestContext.Current.CancellationToken);

        _server.LogEntries.Should()
            .ContainSingle()
            .Which.RequestMessage!.Path.Should()
            .Be("/jira/rest/api/2/myself");
    }

    [Fact]
    public async Task Confluence_request_keeps_context_path()
    {
        _server
            .Given(Request.Create().WithPath("/wiki/rest/api/user/current").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { username = "tester" }));

        await using var provider = BuildProvider(jiraPath: "/jira", confluencePath: "/wiki");

        await provider.GetRequiredService<IConfluenceClient>().CheckAvailabilityAsync(TestContext.Current.CancellationToken);

        _server.LogEntries.Should()
            .ContainSingle()
            .Which.RequestMessage!.Path.Should()
            .Be("/wiki/rest/api/user/current");
    }

    [Fact]
    public async Task Each_system_uses_its_own_token()
    {
        _server
            .Given(Request.Create().WithPath("/jira/rest/api/2/myself").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { name = "tester" }));
        _server
            .Given(Request.Create().WithPath("/wiki/rest/api/user/current").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { username = "tester" }));

        await using var provider = BuildProvider(jiraPath: "/jira", confluencePath: "/wiki");

        await provider.GetRequiredService<IJiraClient>().CheckAvailabilityAsync(TestContext.Current.CancellationToken);
        await provider.GetRequiredService<IConfluenceClient>().CheckAvailabilityAsync(TestContext.Current.CancellationToken);

        var entries = _server.LogEntries.ToList();

        entries.Should().HaveCount(2);

        entries[0].RequestMessage!.Headers!["Authorization"].Should()
            .ContainSingle()
            .Which.Should().Be("Bearer jira-token");

        entries[1].RequestMessage!.Headers!["Authorization"].Should()
            .ContainSingle()
            .Which.Should().Be("Bearer confluence-token");
    }

    public void Dispose() => _server.Stop();

    private ServiceProvider BuildProvider(string jiraPath, string confluencePath)
    {
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri(_server.Url! + jiraPath),
            JiraPat = "jira-token",
            ConfluenceUrl = new Uri(_server.Url! + confluencePath),
            ConfluencePat = "confluence-token"
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Options.Create(options));
        services.AddAtlassianHttpClients();

        return services.BuildServiceProvider();
    }
}



