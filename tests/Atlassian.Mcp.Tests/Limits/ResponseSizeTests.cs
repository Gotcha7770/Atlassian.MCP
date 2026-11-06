using System.Text.Json;
using Atlassian.Mcp.Core.Common;
using Atlassian.Mcp.Core.Models.Jira;

namespace Atlassian.Mcp.Tests.Limits;

public sealed class ResponseSizeTests
{
    [Fact]
    public void Truncates_long_description_to_text_budget()
    {
        var description = new string('x', ResponseLimiter.MaxTextBytes * 2);

        var details = JiraIssueDetailsLimiter.Limit(new JiraIssueDetails
        {
            Key = "PROJ-123",
            Description = description
        });

        details.Description.Should().HaveLength(ResponseLimiter.MaxTextBytes);
        details.Truncated.Should().BeTrue();
        details.Truncation.Should().NotBeNull();
        details.Truncation!.Field.Should().Be("description");
        details.Truncation.OriginalLength.Should().Be(description.Length);
    }

    [Fact]
    public void Truncates_description_by_utf8_bytes()
    {
        var description = new string('ж', ResponseLimiter.MaxTextBytes);

        var details = JiraIssueDetailsLimiter.Limit(new JiraIssueDetails
        {
            Key = "PROJ-123",
            Description = description
        });

        details.Description.Should().NotBeNull();
        details.Description!.Length.Should().BeLessThan(description.Length);
        ResponseLimiter.GetUtf8Length(details.Description).Should().BeLessThanOrEqualTo(ResponseLimiter.MaxTextBytes);
        details.Truncation.Should().NotBeNull();
        details.Truncation!.Field.Should().Be("description");
    }

    [Fact]
    public void Drops_custom_field_that_exceeds_budget_and_keeps_the_rest()
    {
        var huge = new string('x', ResponseLimiter.MaxCustomFieldsBytes + 1);

        var details = JiraIssueDetailsLimiter.Limit(new JiraIssueDetails
        {
            Key = "PROJ-123",
            CustomFields = new Dictionary<string, JsonElement>
            {
                ["customfield_10101"] = JsonDocument.Parse($"\"{huge}\"").RootElement,
                ["customfield_10202"] = JsonDocument.Parse("3").RootElement
            }
        });

        details.CustomFields.Should().NotBeNull();
        details.CustomFields!.Should().ContainKey("customfield_10202");
        details.CustomFields.Should().NotContainKey("customfield_10101");
        details.Truncated.Should().BeTrue();
        details.Truncation.Should().NotBeNull();
        details.Truncation!.Field.Should().Be("customFields");
        details.Truncation.OriginalLength.Should().Be(2);
        details.Truncation.AppliedLength.Should().Be(1);
    }

    [Fact]
    public void Keeps_only_maximum_number_of_custom_fields()
    {
        var customFields = new Dictionary<string, JsonElement>();

        for (var index = 0; index <= ResponseLimiter.MaxItems; index++)
        {
            customFields[$"customfield_{index}"] = JsonDocument.Parse("1").RootElement;
        }

        var details = JiraIssueDetailsLimiter.Limit(new JiraIssueDetails
        {
            Key = "PROJ-123",
            CustomFields = customFields
        });

        details.CustomFields.Should().HaveCount(ResponseLimiter.MaxItems);
        details.Truncation.Should().NotBeNull();
        details.Truncation!.Field.Should().Be("customFields");
    }

    [Fact]
    public void Shares_item_limit_between_custom_fields_links_and_subtasks()
    {
        var customFields = new Dictionary<string, JsonElement>();

        for (var index = 0; index < 30; index++)
        {
            customFields[$"customfield_{index}"] = JsonDocument.Parse("1").RootElement;
        }

        var links = Enumerable
            .Range(0, 60)
            .Select(index => new JiraIssueLink
            {
                Type = "Blocks",
                Direction = "outward",
                Key = $"PROJ-{index}"
            })
            .ToList();

        var subtasks = Enumerable
            .Range(0, 60)
            .Select(index => new JiraSubtask
            {
                Key = $"PROJ-{index}"
            })
            .ToList();

        var details = JiraIssueDetailsLimiter.Limit(new JiraIssueDetails
        {
            Key = "PROJ-123",
            CustomFields = customFields,
            Links = links,
            Subtasks = subtasks
        });

        details.CustomFields.Should().BeNull();
        details.Links.Should().HaveCount(60);
        details.Subtasks.Should().HaveCount(40);
        details.Truncated.Should().BeTrue();
        details.Truncation.Should().NotBeNull();
        details.Truncation!.Field.Should().Be("subtasks");
        details.Truncation.OriginalLength.Should().Be(subtasks.Count);
        details.Truncation.AppliedLength.Should().Be(40);
    }

    [Fact]
    public void Keeps_small_response_unchanged()
    {
        var details = JiraIssueDetailsLimiter.Limit(new JiraIssueDetails
        {
            Key = "PROJ-123",
            Summary = "CSV export",
            CustomFields = new Dictionary<string, JsonElement>
            {
                ["customfield_10101"] = JsonDocument.Parse("""{"value":"Backend"}""").RootElement
            }
        });

        details.CustomFields.Should().ContainKey("customfield_10101");
        details.Truncated.Should().BeFalse();
        details.Truncation.Should().BeNull();
    }
}
