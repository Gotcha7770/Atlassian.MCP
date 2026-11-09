using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using Atlassian.Mcp.Core.Contracts;
using Atlassian.Mcp.Core.Errors;
using Atlassian.Mcp.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using static AwesomeAssertions.FluentActions;

namespace Atlassian.Mcp.IntegrationTests.Http;

public sealed class RetryOn429Tests : IClassFixture<HttpClientTestFixture>
{
    private readonly HttpClientTestFixture _fixture;

    public RetryOn429Tests(HttpClientTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Retries_once_after_429_and_returns_success()
    {
        var mock = new MockHttpMessageHandler();
        var calls = 0;
        var request = mock
            .When("https://jira.example.com/rest/api/2/myself")
            .Respond(_ =>
            {
                calls++;

                if (calls == 1)
                {
                    var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                    response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);

                    return response;
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"name":"tester"}""",
                        Encoding.UTF8,
                        MediaTypeNames.Application.Json)
                };
            });

        await using var provider = _fixture.CreateProvider(CreateOptions(), jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        await client.CheckAvailability(TestContext.Current.CancellationToken);

        mock.GetMatchCount(request).Should().Be(2);
    }

    [Fact]
    public async Task Does_not_retry_when_server_asks_to_wait_longer_than_limit()
    {
        var mock = new MockHttpMessageHandler();
        var request = mock
            .When("https://jira.example.com/rest/api/2/myself")
            .Respond(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(60));
                response.Content = new StringContent(
                    """{"errorMessages":["Rate limit exceeded."]}""",
                    Encoding.UTF8,
                    MediaTypeNames.Application.Json);

                return response;
            });

        await using var provider = _fixture.CreateProvider(CreateOptions(), jiraHandler: mock);
        var client = provider.GetRequiredService<IJiraClient>();

        await Awaiting(() => client.CheckAvailability(TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.StatusCode == HttpStatusCode.TooManyRequests);

        mock.GetMatchCount(request).Should().Be(1);
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