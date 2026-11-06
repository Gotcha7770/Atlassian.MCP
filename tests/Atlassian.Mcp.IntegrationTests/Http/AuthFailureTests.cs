using System.Net;
using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Infrastructure;
using Atlassian.Mcp.Core.Errors;
using Atlassian.Mcp.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using static AwesomeAssertions.FluentActions;

namespace Atlassian.Mcp.IntegrationTests.Http;

public sealed class AuthFailureTests
{
    [Fact]
    public async Task Jira_401_is_mapped_to_api_exception_with_status_and_message()
    {
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://jira.example.com/rest/api/2/myself")
            .Respond(
                HttpStatusCode.Unauthorized,
                "application/json",
                """{"errorMessages":["You are not authenticated."]}""");

        await using var provider = BuildProvider(jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        await Awaiting(() => client.CheckAvailabilityAsync(TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.System == AtlassianSystem.Jira)
            .Where(error => error.StatusCode == HttpStatusCode.Unauthorized)
            .Where(error => error.Details == "You are not authenticated.")
            .Where(error => error.Message.Contains("401"))
            .Where(error => error.Message.Contains("You are not authenticated."));

        mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public async Task Confluence_403_is_mapped_to_api_exception_with_status_and_message()
    {
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://confluence.example.com/rest/api/user/current")
            .Respond(
                HttpStatusCode.Forbidden,
                "application/json",
                """{"statusCode":403,"message":"Insufficient permissions"}""");

        await using var provider = BuildProvider(confluenceHandler: mock);
        var client = provider.GetRequiredService<IConfluenceClient>();

        await Awaiting(() => client.CheckAvailabilityAsync(TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.System == AtlassianSystem.Confluence)
            .Where(error => error.StatusCode == HttpStatusCode.Forbidden)
            .Where(error => error.Details == "Insufficient permissions");

        mock.GetMatchCount(request).Should().Be(1);
    }

    private static ServiceProvider BuildProvider(
        HttpMessageHandler? jiraHandler = null,
        HttpMessageHandler? confluenceHandler = null)
    {
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri("https://jira.example.com"),
            JiraPat = "jira-token",
            ConfluenceUrl = new Uri("https://confluence.example.com"),
            ConfluencePat = "confluence-token"
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Options.Create(options));
        services.AddAtlassianHttpClients(jiraHandler, confluenceHandler);

        return services.BuildServiceProvider();
    }
}


