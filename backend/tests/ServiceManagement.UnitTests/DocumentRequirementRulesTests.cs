using FluentAssertions;
using ServiceManagement.Domain.Services;

namespace ServiceManagement.UnitTests;

public class DocumentRequirementRulesTests
{
    [Fact]
    public void Category_wide_requirement_applies_when_any_service_in_that_category_is_selected()
    {
        var selected = new List<(Guid ServiceId, Guid CategoryId)>
        {
            (Guid.NewGuid(), Guid.NewGuid())
        };
        var categoryId = selected[0].CategoryId;

        DocumentRequirementRules.AppliesToSelection(categoryId, [], selected).Should().BeTrue();
        DocumentRequirementRules.AppliesToSelection(Guid.NewGuid(), [], selected).Should().BeFalse();
    }

    [Fact]
    public void Scoped_requirement_applies_only_to_listed_services()
    {
        var walk = Guid.NewGuid();
        var vehicle = Guid.NewGuid();
        var category = Guid.NewGuid();
        var selectedWalk = new List<(Guid ServiceId, Guid CategoryId)> { (walk, category) };
        var selectedVehicle = new List<(Guid ServiceId, Guid CategoryId)> { (vehicle, category) };

        DocumentRequirementRules.AppliesToSelection(category, [vehicle], selectedWalk).Should().BeFalse();
        DocumentRequirementRules.AppliesToSelection(category, [vehicle], selectedVehicle).Should().BeTrue();
    }
}
