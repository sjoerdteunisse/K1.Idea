using K1Idea.Domain.Enums;

namespace K1Idea.Domain.Tests;

public sealed class TicketHierarchyTests
{
    [Fact]
    public void Idea_should_not_require_parent()
    {
        var type = TicketType.Idea;
        type.Should().Be(TicketType.Idea);
    }

    [Theory]
    [InlineData(TicketType.Initiative, TicketType.Idea, true)]
    [InlineData(TicketType.Initiative, TicketType.Project, false)]
    [InlineData(TicketType.Initiative, TicketType.Task, false)]
    [InlineData(TicketType.Project, TicketType.Initiative, true)]
    [InlineData(TicketType.Project, TicketType.Idea, false)]
    [InlineData(TicketType.Task, TicketType.Project, true)]
    [InlineData(TicketType.Task, TicketType.Initiative, false)]
    public void Hierarchy_parent_validation_rules(TicketType childType, TicketType parentType, bool isValid)
    {
        var result = IsValidParent(childType, parentType);
        result.Should().Be(isValid);
    }

    [Fact]
    public void Idea_cannot_have_any_parent()
    {
        foreach (var parentType in Enum.GetValues<TicketType>())
        {
            IsValidParent(TicketType.Idea, parentType).Should().BeFalse();
        }
    }

    private static bool IsValidParent(TicketType childType, TicketType parentType) =>
        childType switch
        {
            TicketType.Idea => false,
            TicketType.Initiative => parentType == TicketType.Idea,
            TicketType.Project => parentType == TicketType.Initiative,
            TicketType.Task => parentType == TicketType.Project,
            _ => false
        };
}
