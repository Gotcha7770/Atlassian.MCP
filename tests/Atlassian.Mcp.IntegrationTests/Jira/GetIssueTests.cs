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

namespace Atlassian.Mcp.IntegrationTests.Jira;

public sealed class GetIssueTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    [Fact]
    public async Task Requests_all_fields_when_list_is_not_specified()
    {
        _server
            .Given(Request.Create().WithPath("/rest/api/2/issue/PROJ-123").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new
                {
                    key = "PROJ-123",
                    fields = new { summary = "CSV export", customfield_10101 = new { value = "Backend" } }
                }));

        await using var provider = BuildProvider("/");
        var client = provider.GetRequiredService<IJiraClient>();

        var issue = await client.GetIssueAsync("PROJ-123", null, TestContext.Current.CancellationToken);

        issue.Key.Should().Be("PROJ-123");
        issue.CustomFields.Should().ContainKey("customfield_10101");
        _server.LogEntries.Should()
            .ContainSingle()
            .Which.RequestMessage!.Query!["fields"].Should().ContainSingle().Which.Should().Be("*all");
    }

    [Fact]
    public async Task Requests_explicit_field_list()
    {
        _server
            .Given(Request.Create().WithPath("/rest/api/2/issue/PROJ-123").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { key = "PROJ-123", fields = new { summary = "CSV export" } }));

        await using var provider = BuildProvider("/");
        var client = provider.GetRequiredService<IJiraClient>();

        await client.GetIssueAsync(
            "PROJ-123",
            ["summary", "status"],
            TestContext.Current.CancellationToken);

        _server.LogEntries.Should()
            .ContainSingle()
            .Which.RequestMessage!.Query!["fields"].Should().ContainSingle().Which.Should().Be("summary,status");
    }

    [Fact]
    public async Task Keeps_context_path()
    {
        _server
            .Given(Request.Create().WithPath("/jira/rest/api/2/issue/PROJ-123").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { key = "PROJ-123", fields = new { summary = "CSV export" } }));

        await using var provider = BuildProvider("/jira");
        var client = provider.GetRequiredService<IJiraClient>();

        var issue = await client.GetIssueAsync("PROJ-123", null, TestContext.Current.CancellationToken);

        issue.Summary.Should().Be("CSV export");
        _server.LogEntries.Should()
            .ContainSingle()
            .Which.RequestMessage!.Path.Should().Be("/jira/rest/api/2/issue/PROJ-123");
    }

    [Theory]
    [InlineData(404, HttpStatusCode.NotFound)]
    [InlineData(403, HttpStatusCode.Forbidden)]
    public async Task Maps_error_status_to_api_exception(int status, HttpStatusCode expected)
    {
        _server
            .Given(Request.Create().WithPath("/rest/api/2/issue/PROJ-123").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(status)
                .WithBodyAsJson(new { errorMessages = new[] { "Issue does not exist." } }));

        await using var provider = BuildProvider("/");
        var client = provider.GetRequiredService<IJiraClient>();

        await Awaiting(() => client.GetIssueAsync("PROJ-123", null, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>()
            .Where(error => error.StatusCode == expected)
            .Where(error => error.Details == "Issue does not exist.");
    }

    [Fact]
    public async Task Empty_response_is_reported_as_error()
    {
        _server
            .Given(Request.Create().WithPath("/rest/api/2/issue/PROJ-123").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(204));

        await using var provider = BuildProvider("/");
        var client = provider.GetRequiredService<IJiraClient>();

        await Awaiting(() => client.GetIssueAsync("PROJ-123", null, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<AtlassianApiException>();
    }

    public void Dispose() => _server.Stop();

    private ServiceProvider BuildProvider(string contextPath)
    {
        var options = new AtlassianOptions
        {
            JiraUrl = new Uri(_server.Url! + contextPath),
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
