using System.Text.Json;
using Atlassian.Mcp.Core.Common;
using Atlassian.Mcp.Core.Models.Jira;

namespace Atlassian.Mcp.Tests.Limits;

public sealed class IssueSearchResponseLimiterTests
{
    [Fact]
    public void Truncates_oversized_utf8_text_and_reports_value_size_reason()
    {
        string description = new('ж', ResponseLimiter.MaxTextBytes);
        string json = JsonSerializer.Serialize(description);
        using var document = JsonDocument.Parse(json);
        var response = new IssueSearchResponse
        {
            StartAt = 12,
            MaxResults = 20,
            Total = 40,
            ReturnedCount = 1,
            HasMore = true,
            NextStartAt = 13,
            Issues =
            [
                new IssueSearchItem
                {
                    Key = "PROJ-145",
                    Id = "10145",
                    Fields = new Dictionary<string, JsonElement>
                    {
                        ["description"] = document.RootElement.Clone()
                    }
                }
            ]
        };

        var limited = IssueSearchResponseLimiter.Limit(response);

        limited.Issues.Should()
            .ContainSingle()
            .Which.Should()
            .Satisfy<IssueSearchItem>(issue =>
            {
                issue.Key.Should().Be("PROJ-145");
                string? limitedDescription = issue.Fields["description"].GetString();
                limitedDescription.Should().NotBeNull();
                limitedDescription!.Length.Should().BeLessThan(description.Length);
                limitedDescription.Utf8Length.Should()
                    .BeLessThanOrEqualTo(ResponseLimiter.MaxTextBytes);
                issue.TruncatedFields.Should()
                    .ContainSingle()
                    .Which.Should()
                    .Satisfy<TruncatedField>(truncatedField =>
                    {
                        truncatedField.Name.Should().Be("description");
                        truncatedField.Reason.Should().Be("value-size-limit");
                    });
            });

        limited.StartAt.Should().Be(12);
        limited.Total.Should().Be(40);
        limited.ReturnedCount.Should().Be(1);
        limited.HasMore.Should().BeTrue();
        limited.NextStartAt.Should().Be(13);
    }

    [Fact]
    public void Drops_oversized_dynamic_json_value_and_reports_value_size_reason()
    {
        string largeValue = new('x', ResponseLimiter.MaxCustomFieldBytes);
        string json = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["value"] = largeValue
        });
        using var document = JsonDocument.Parse(json);
        var response = new IssueSearchResponse
        {
            StartAt = 0,
            MaxResults = 20,
            Total = 1,
            ReturnedCount = 1,
            HasMore = false,
            Issues =
            [
                new IssueSearchItem
                {
                    Key = "PROJ-146",
                    Fields = new Dictionary<string, JsonElement>
                    {
                        ["customfield_14002"] = document.RootElement.Clone()
                    }
                }
            ]
        };

        var limited = IssueSearchResponseLimiter.Limit(response);

        limited.Issues.Should()
            .ContainSingle()
            .Which.Should()
            .Satisfy<IssueSearchItem>(issue =>
            {
                issue.Key.Should().Be("PROJ-146");
                issue.Fields.Should().NotContainKey("customfield_14002");
                issue.TruncatedFields.Should()
                    .ContainSingle()
                    .Which.Should()
                    .Satisfy<TruncatedField>(truncatedField =>
                    {
                        truncatedField.Name.Should().Be("customfield_14002");
                        truncatedField.Reason.Should().Be("value-size-limit");
                    });
            });
        limited.HasMore.Should().BeFalse();
        limited.NextStartAt.Should().BeNull();
    }

    [Fact]
    public void Limits_fields_to_one_hundred_and_reports_the_omitted_field()
    {
        using var document = JsonDocument.Parse("1");
        var fields = new Dictionary<string, JsonElement>();

        for (var index = 0; index <= ResponseLimiter.MaxItems; index++)
        {
            fields[$"customfield_{index}"] = document.RootElement.Clone();
        }

        var response = new IssueSearchResponse
        {
            StartAt = 5,
            MaxResults = 10,
            Total = 20,
            ReturnedCount = 1,
            HasMore = true,
            NextStartAt = 6,
            Issues =
            [
                new IssueSearchItem
                {
                    Key = "PROJ-147",
                    Fields = fields
                }
            ]
        };

        var limited = IssueSearchResponseLimiter.Limit(response);

        limited.Issues.Should()
            .ContainSingle()
            .Which.Should()
            .Satisfy<IssueSearchItem>(issue =>
            {
                issue.Fields.Should().HaveCount(ResponseLimiter.MaxItems);
                issue.Fields.Should().NotContainKey("customfield_100");
                issue.TruncatedFields.Should()
                    .ContainSingle()
                    .Which.Should()
                    .Satisfy<TruncatedField>(truncatedField =>
                    {
                        truncatedField.Name.Should().Be("customfield_100");
                        truncatedField.Reason.Should().Be("field-count-limit");
                    });
                issue.Key.Should().Be("PROJ-147");
            });
        limited.StartAt.Should().Be(5);
        limited.HasMore.Should().BeTrue();
        limited.NextStartAt.Should().Be(6);
    }
}