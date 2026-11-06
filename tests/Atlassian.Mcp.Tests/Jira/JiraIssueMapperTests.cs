using System.Text.Json;
using Atlassian.Mcp.Infrastructure.Jira;
using Atlassian.Mcp.Infrastructure.Jira.Models;

namespace Atlassian.Mcp.Tests.Jira;

public sealed class JiraIssueMapperTests
{
    [Fact]
    public void Maps_typed_fields()
    {
        const string body = """
        {
          "id": "10001",
          "key": "PROJ-123",
          "fields": {
            "summary": "CSV export",
            "status": { "name": "In Progress" },
            "issuetype": { "name": "Story" },
            "project": { "key": "PROJ" },
            "assignee": { "displayName": "Ivan Petrov" },
            "reporter": { "displayName": "Maria Sidorova" },
            "priority": { "name": "High" },
            "created": "2026-10-01T09:12:00.000+0000",
            "updated": "2026-11-03T16:40:11.000+0000",
            "labels": ["reporting"]
          }
        }
        """;

        var issue = JsonSerializer.Deserialize<JiraIssue>(body, JsonSerializerOptions.Web);

        var details = JiraIssueMapper.ToDetails(issue!);

        details.Key.Should().Be("PROJ-123");
        details.Id.Should().Be("10001");
        details.Summary.Should().Be("CSV export");
        details.Status.Should().Be("In Progress");
        details.IssueType.Should().Be("Story");
        details.ProjectKey.Should().Be("PROJ");
        details.Assignee.Should().Be("Ivan Petrov");
        details.Reporter.Should().Be("Maria Sidorova");
        details.Priority.Should().Be("High");
        details.Updated.Should().Be("2026-11-03T16:40:11.000+0000");
        details.Labels.Should().ContainSingle().Which.Should().Be("reporting");
    }

    [Fact]
    public void Keeps_only_custom_fields()
    {
        const string body = """
        {
          "key": "PROJ-123",
          "fields": {
            "summary": "CSV export",
            "customfield_10101": { "value": "Backend" },
            "customfield_10202": 3,
            "customfield_10303": null,
            "workratio": 42
          }
        }
        """;

        var issue = JsonSerializer.Deserialize<JiraIssue>(body, JsonSerializerOptions.Web);

        var details = JiraIssueMapper.ToDetails(issue!);

        details.CustomFields.Should().NotBeNull();
        details.CustomFields!.Should().ContainKeys("customfield_10101", "customfield_10202");
        details.CustomFields.Should().NotContainKey("workratio");
        details.CustomFields.Should().NotContainKey("customfield_10303");
        details.CustomFields["customfield_10101"].GetProperty("value").GetString().Should().Be("Backend");
        details.CustomFields["customfield_10202"].GetInt32().Should().Be(3);
    }

    [Fact]
    public void Custom_field_with_unexpected_type_does_not_break_deserialization()
    {
        const string body = """
        {
          "key": "PROJ-123",
          "fields": {
            "customfield_10101": "plain string instead of object"
          }
        }
        """;

        var issue = JsonSerializer.Deserialize<JiraIssue>(body, JsonSerializerOptions.Web);

        var details = JiraIssueMapper.ToDetails(issue!);

        details.CustomFields.Should().ContainKey("customfield_10101");
        details.CustomFields!["customfield_10101"].ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public void Null_and_missing_fields_are_distinguished_from_values()
    {
        const string body = """
        {
          "key": "PROJ-123",
          "fields": {
            "summary": "CSV export",
            "description": null,
            "labels": []
          }
        }
        """;

        var issue = JsonSerializer.Deserialize<JiraIssue>(body, JsonSerializerOptions.Web);

        var details = JiraIssueMapper.ToDetails(issue!);

        details.Description.Should().BeNull();
        details.Status.Should().BeNull();
        details.Labels.Should().BeEmpty();
        details.CustomFields.Should().BeNull();
    }
}
