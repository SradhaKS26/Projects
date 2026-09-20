namespace ServiceManagement.Domain.Enums;

/// <summary>
/// A provider applies for each catalog service they want to fulfill.
/// Only <see cref="Approved"/> registrations count toward request eligibility.
/// </summary>
public enum ProviderServiceStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
