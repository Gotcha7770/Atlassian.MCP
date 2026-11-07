using System.Text.Json;
using Atlassian.Mcp.Infrastructure.Jira;
using Atlassian.Mcp.Infrastructure.Jira.Models;
using CoreIssueDetails = Atlassian.Mcp.Core.Models.Jira.IssueDetails;
using CoreIssueLink = Atlassian.Mcp.Core.Models.Jira.IssueLink;
using CoreSubtask = Atlassian.Mcp.Core.Models.Jira.Subtask;

namespace Atlassian.Mcp.Tests.Jira;

public sealed class IssueMapperTests
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

        var issue = JsonSerializer.Deserialize<Issue>(body, JsonSerializerOptions.Web);

        var details = IssueMapper.ToDetails(issue!);

        details
            .Should()
            .BeEquivalentTo(
                new CoreIssueDetails
                {
                    Key = "PROJ-123",
                    Id = "10001",
                    Summary = "CSV export",
                    Status = "In Progress",
                    IssueType = "Story",
                    ProjectKey = "PROJ",
                    Assignee = "Ivan Petrov",
                    Reporter = "Maria Sidorova",
                    Priority = "High",
                    Created = "2026-10-01T09:12:00.000+0000",
                    Updated = "2026-11-03T16:40:11.000+0000",
                    Labels = ["reporting"]
                });
    }

    [Fact]
    public void Maps_subtasks()
    {
        const string body = """
        {
          "key": "PROJ-123",
          "fields": {
            "summary": "CSV export",
            "subtasks": [
              {
                "id": "10962",
                "key": "PROJ-94",
                "fields": {
                  "summary": "Create endpoint",
                  "status": { "name": "Done" }
                }
              }
            ]
          }
        }
        """;

        var issue = JsonSerializer.Deserialize<Issue>(body, JsonSerializerOptions.Web);

        var details = IssueMapper.ToDetails(issue!);

        details.Subtasks.Should()
            .ContainSingle()
            .Which.Should()
            .Satisfy<CoreSubtask>(subtask =>
            {
                subtask.Key.Should().Be("PROJ-94");
                subtask.Summary.Should().Be("Create endpoint");
                subtask.Status.Should().Be("Done");
            });
    }

    [Fact]
    public void Maps_only_blocking_links()
    {
        const string body = """
        {
          "key": "PROJ-123",
          "fields": {
            "summary": "CSV export",
            "issuelinks": [
              {
                "type": { "name": "Blocks", "inward": "is blocked by", "outward": "blocks" },
                "outwardIssue": {
                  "key": "PROJ-200",
                  "fields": { "summary": "Release", "status": { "name": "To Do" } }
                }
              },
              {
                "type": { "name": "Blocks", "inward": "is blocked by", "outward": "blocks" },
                "inwardIssue": {
                  "key": "PROJ-100",
                  "fields": { "summary": "Design", "status": { "name": "Done" } }
                }
              },
              {
                "type": { "name": "Relates", "inward": "relates to", "outward": "relates to" },
                "outwardIssue": {
                  "key": "PROJ-300",
                  "fields": { "summary": "Unrelated", "status": { "name": "To Do" } }
                }
              }
            ]
          }
        }
        """;

        var issue = JsonSerializer.Deserialize<Issue>(body, JsonSerializerOptions.Web);

        var details = IssueMapper.ToDetails(issue!);

        details
            .Should()
            .BeEquivalentTo(
                new CoreIssueDetails
                {
                    Key = "PROJ-123",
                    Summary = "CSV export",
                    Links =
                    [
                        new CoreIssueLink
                        {
                            Type = "Blocks",
                            Direction = "outward",
                            Key = "PROJ-200",
                            Summary = "Release",
                            Status = "To Do"
                        },
                        new CoreIssueLink
                        {
                            Type = "Blocks",
                            Direction = "inward",
                            Key = "PROJ-100",
                            Summary = "Design",
                            Status = "Done"
                        }
                    ]
                });
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
            "customfield_10404": [],
            "customfield_10505": {},
            "workratio": 42
          }
        }
        """;

        var issue = JsonSerializer.Deserialize<Issue>(body, JsonSerializerOptions.Web);

        var details = IssueMapper.ToDetails(issue!);

        details.CustomFields.Should().NotBeNull();
        details.CustomFields!.Should().ContainKeys("customfield_10101", "customfield_10202");
        details.CustomFields.Should().NotContainKey("workratio");
        details.CustomFields.Should().NotContainKey("customfield_10303");
        details.CustomFields.Should().NotContainKey("customfield_10404");
        details.CustomFields.Should().NotContainKey("customfield_10505");
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

        var issue = JsonSerializer.Deserialize<Issue>(body, JsonSerializerOptions.Web);

        var details = IssueMapper.ToDetails(issue!);

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

        var issue = JsonSerializer.Deserialize<Issue>(body, JsonSerializerOptions.Web);

        var details = IssueMapper.ToDetails(issue!);

        details
            .Should()
            .BeEquivalentTo(
                new CoreIssueDetails
                {
                    Key = "PROJ-123",
                    Summary = "CSV export",
                    Labels = []
                });
    }
}
