using System.Text.Json;
using Atlassian.Mcp.Core.Models.Jira;
using Atlassian.Mcp.Infrastructure.Jira;
using Atlassian.Mcp.Infrastructure.Jira.Models;

namespace Atlassian.Mcp.Tests.Jira;

public sealed class IssueSearchMapperTests
{
    [Fact]
    public void Preserves_dynamic_field_names_and_values()
    {
        using var statusDocument = JsonDocument.Parse(
            """{"name":"Waiting for review","statusCategory":{"key":"review","rank":7}}""");
        using var issueTypeDocument = JsonDocument.Parse(
            """{"name":"Capability","subtask":true,"hierarchyLevel":3}""");
        using var customFieldDocument = JsonDocument.Parse(
            """[{"value":"Backend","weight":2},{"value":"Platform","weight":4}]""");
        using var workRatioDocument = JsonDocument.Parse("63");

        var fields = new Dictionary<string, JsonElement>
        {
            ["status"] = statusDocument.RootElement.Clone(),
            ["issuetype"] = issueTypeDocument.RootElement.Clone(),
            ["customfield_14001"] = customFieldDocument.RootElement.Clone(),
            ["workratio"] = workRatioDocument.RootElement.Clone()
        };
        var page = new IssueSearchPage
        {
            StartAt = 0,
            MaxResults = 1,
            Total = 1,
            Issues =
            [
                new IssueSearchResultIssue
                {
                    Id = "10140",
                    Key = "PROJ-140",
                    Fields = fields
                }
            ]
        };

        var result = IssueSearchMapper.ToResponse(page);

        result.Issues.Should()
            .ContainSingle()
            .Which.Should()
            .Satisfy<IssueSearchItem>(issue =>
            {
                issue.Key.Should().Be("PROJ-140");
                issue.Fields.Should()
                    .ContainKeys("status", "issuetype", "customfield_14001", "workratio");

                JsonElement status = issue.Fields["status"];
                status.GetProperty("statusCategory").GetProperty("key").GetString()
                    .Should().Be("review");
                status.GetProperty("statusCategory").GetProperty("rank").GetInt32()
                    .Should().Be(7);

                JsonElement issueType = issue.Fields["issuetype"];
                issueType.GetProperty("subtask").GetBoolean().Should().BeTrue();
                issueType.GetProperty("hierarchyLevel").GetInt32().Should().Be(3);

                JsonElement customField = issue.Fields["customfield_14001"];
                customField.GetArrayLength().Should().Be(2);
                customField[1].GetProperty("value").GetString().Should().Be("Platform");

                issue.Fields["workratio"].GetInt32().Should().Be(63);
            });
    }

    [Fact]
    public void Computes_next_offset_from_returned_issue_count()
    {
        var page = new IssueSearchPage
        {
            StartAt = 8,
            MaxResults = 1,
            Total = 12,
            Issues =
            [
                new IssueSearchResultIssue
                {
                    Id = "10109",
                    Key = "PROJ-109",
                    Fields = new Dictionary<string, JsonElement>()
                }
            ]
        };

        var result = IssueSearchMapper.ToResponse(page);

        result.StartAt.Should().Be(8);
        result.MaxResults.Should().Be(1);
        result.Total.Should().Be(12);
        result.ReturnedCount.Should().Be(1);
        result.HasMore.Should().BeTrue();
        result.NextStartAt.Should().Be(9);
    }

    [Fact]
    public void Omits_next_offset_when_the_page_has_no_more_results()
    {
        var page = new IssueSearchPage
        {
            StartAt = 9,
            MaxResults = 1,
            Total = 10,
            Issues =
            [
                new IssueSearchResultIssue
                {
                    Id = "10110",
                    Key = "PROJ-110",
                    Fields = new Dictionary<string, JsonElement>()
                }
            ]
        };

        var result = IssueSearchMapper.ToResponse(page);

        result.ReturnedCount.Should().Be(1);
        result.HasMore.Should().BeFalse();
        result.NextStartAt.Should().BeNull();
    }
}