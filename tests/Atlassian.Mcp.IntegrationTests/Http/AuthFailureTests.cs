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

public sealed class AuthFailureTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    [Fact]
    public async Task Jira_401_is_mapped_to_api_exception_with_status_and_message()
    {
        _server
            .Given(Request.Create().WithPath("/rest/api/2/myself").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBodyAsJson(new { errorMessages = new[] { "You are not authenticated." } }));

        await using var provider = BuildProvider();
        var client = provider.GetRequiredService<IJiraClient>();

        await Awaiting(() => client.CheckAvailabilityAsync(TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.System == AtlassianSystem.Jira)
            .Where(error => error.StatusCode == HttpStatusCode.Unauthorized)
            .Where(error => error.Details == "You are not authenticated.")
            .Where(error => error.Message.Contains("401"))
            .Where(error => error.Message.Contains("You are not authenticated."));
    }

    [Fact]
    public async Task Confluence_403_is_mapped_to_api_exception_with_status_and_message()
    {
        _server
            .Given(Request.Create().WithPath("/rest/api/user/current").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(403)
                .WithBodyAsJson(new { statusCode = 403, message = "Insufficient permissions" }));

        await using var provider = BuildProvider();
        var client = provider.GetRequiredService<IConfluenceClient>();

        await Awaiting(() => client.CheckAvailabilityAsync(TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.System == AtlassianSystem.Confluence)
            .Where(error => error.StatusCode == HttpStatusCode.Forbidden)
            .Where(error => error.Details == "Insufficient permissions");
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
        services.AddSingleton(Options.Create(options));
        services.AddAtlassianHttpClients();

        return services.BuildServiceProvider();
    }
}




