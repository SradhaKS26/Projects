namespace ServiceManagement.Domain.Services;

public static class DocumentRequirementRules
{
    /// <summary>
    /// A requirement with no service scopes applies to every selected service in
    /// its category. A scoped requirement applies only when the provider selected
    /// one of those services (for example a driving licence for vehicle delivery).
    /// </summary>
    public static bool AppliesToSelection(
        Guid requirementCategoryId,
        IReadOnlyCollection<Guid> scopedServiceIds,
        IReadOnlyCollection<(Guid ServiceId, Guid CategoryId)> selectedServices)
    {
        var selectedInCategory = selectedServices
            .Where(s => s.CategoryId == requirementCategoryId)
            .Select(s => s.ServiceId)
            .ToHashSet();

        if (selectedInCategory.Count == 0)
        {
            return false;
        }

        if (scopedServiceIds.Count == 0)
        {
            return true;
        }

        return scopedServiceIds.Any(selectedInCategory.Contains);
    }
}
