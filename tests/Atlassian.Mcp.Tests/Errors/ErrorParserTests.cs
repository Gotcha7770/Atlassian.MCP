using System.Net;
using Atlassian.Mcp.Core.Errors;
using Atlassian.Mcp.Infrastructure.Confluence;
using Atlassian.Mcp.Infrastructure.Jira;

namespace Atlassian.Mcp.Tests.Errors;

public sealed class ErrorParserTests
{
    [Fact]
    public void Jira_parser_reads_error_messages()
    {
        const string body = """{"errorMessages":["Issue does not exist","Check the key"],"errors":{}}""";

        var message = JiraErrorParser.ExtractMessage(body);

        message.Should().Be("Issue does not exist; Check the key");
    }

    [Fact]
    public void Jira_parser_reads_field_errors()
    {
        const string body = """{"errorMessages":[],"errors":{"summary":"You must specify a summary"}}""";

        var message = JiraErrorParser.ExtractMessage(body);

        message.Should().Be("summary: You must specify a summary");
    }

    [Fact]
    public void Confluence_parser_reads_message()
    {
        const string body = """{"statusCode":404,"message":"No content found with id 123"}""";

        var message = ConfluenceErrorParser.ExtractMessage(body);

        message.Should().Be("No content found with id 123");
    }

    [Fact]
    public void Exception_truncates_details_to_limit()
    {
        var details = new string('x', AtlassianApiException.MaxDetailsLength + 500);

        var exception = new AtlassianApiException(AtlassianSystem.Jira, HttpStatusCode.BadRequest, "failed", details);

        exception.Details.Should().HaveLength(AtlassianApiException.MaxDetailsLength);
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.System.Should().Be(AtlassianSystem.Jira);
    }
}
