using System.Net;
using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Core.Errors;
using Atlassian.Mcp.Infrastructure;
using Atlassian.Mcp.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static AwesomeAssertions.FluentActions;

namespace Atlassian.Mcp.IntegrationTests.Http;

public sealed class RetryOn429Tests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    [Fact]
    public async Task Retries_once_after_429_and_returns_success()
    {
        _server
            .Given(Request.Create().WithPath("/rest/api/2/myself").UsingGet())
            .InScenario("jira-retry")
            .WillSetStateTo("second", 1)
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Retry-After", "0"));
        _server
            .Given(Request.Create().WithPath("/rest/api/2/myself").UsingGet())
            .InScenario("jira-retry")
            .WhenStateIs("second")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { name = "tester" }));

        await using var provider = BuildProvider();
        var client = provider.GetRequiredService<IJiraClient>();

        await client.CheckAvailabilityAsync(TestContext.Current.CancellationToken);

        _server.LogEntries.Should().HaveCount(2);
    }

    [Fact]
    public async Task Does_not_retry_when_server_asks_to_wait_longer_than_limit()
    {
        _server
            .Given(Request.Create().WithPath("/rest/api/2/myself").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Retry-After", "60")
                .WithBodyAsJson(new { errorMessages = new[] { "Rate limit exceeded." } }));

        await using var provider = BuildProvider();
        var client = provider.GetRequiredService<IJiraClient>();

        await Awaiting(() => client.CheckAvailabilityAsync(TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.StatusCode == HttpStatusCode.TooManyRequests);

        _server.LogEntries.Should().ContainSingle();
    }

    public void Dispose() => _server.Stop();

    private ServiceProvider BuildProvider()
    {
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri(_server.Url!),
            JiraPat = "jira-token",
            ConfluenceUrl = new Uri(_server.Url!),
            ConfluencePat = "confluence-token"
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<AtlassianOptions>>(Options.Create(options));
        services.AddAtlassianHttpClients();

        return services.BuildServiceProvider();
    }
}


