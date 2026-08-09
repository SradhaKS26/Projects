namespace ServiceManagement.Domain.Enums;

/// <summary>
/// Controlled lifecycle for service requests. Clients cannot set arbitrary statuses;
/// transitions are validated in the application layer.
/// </summary>
public enum ServiceRequestStatus
{
    Pending = 0,
    Accepted = 1,
    Assigned = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    Rejected = 6,
    Failed = 7
}
