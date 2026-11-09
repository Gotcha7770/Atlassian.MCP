using System.Net;
using System.Net.Mime;
using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Core.Errors;
using Atlassian.Mcp.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using static AwesomeAssertions.FluentActions;

namespace Atlassian.Mcp.IntegrationTests.Http;

public sealed class AuthFailureTests : IClassFixture<HttpClientTestFixture>
{
    private readonly HttpClientTestFixture _fixture;

    public AuthFailureTests(HttpClientTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Jira_401_is_mapped_to_api_exception_with_status_and_message()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://jira.example.com/rest/api/2/myself")
            .Respond(
                HttpStatusCode.Unauthorized,
                MediaTypeNames.Application.Json,
                """{"errorMessages":["You are not authenticated."]}""");

        await using var provider = _fixture.CreateProvider(CreateOptions(), jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        await Awaiting(() => client.CheckAvailability(TestContext.Current.CancellationToken))
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
        var mock = new MockHttpMessageHandler();
        mock.When("https://confluence.example.com/rest/api/user/current")
            .Respond(
                HttpStatusCode.Forbidden,
                MediaTypeNames.Application.Json,
                """{"statusCode":403,"message":"Insufficient permissions"}""");

        await using var provider = _fixture.CreateProvider(CreateOptions(), confluenceHandler: mock);
        var client = provider.GetRequiredService<IConfluenceClient>();

        await Awaiting(() => client.CheckAvailabilityAsync(TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.System == AtlassianSystem.Confluence)
            .Where(error => error.StatusCode == HttpStatusCode.Forbidden)
            .Where(error => error.Details == "Insufficient permissions");
    }

    private static AtlassianOptions CreateOptions() =>
        new()
        {
            JiraUrl = new Uri("https://jira.example.com"),
            JiraPat = "jira-token",
            ConfluenceUrl = new Uri("https://confluence.example.com"),
            ConfluencePat = "confluence-token"
        };
}