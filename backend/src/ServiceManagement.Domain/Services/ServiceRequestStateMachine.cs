using ServiceManagement.Domain.Enums;

namespace ServiceManagement.Domain.Services;

/// <summary>
/// Domain rules for valid service-request status transitions.
/// Controllers must never set status directly; use this gate.
/// </summary>
public static class ServiceRequestStateMachine
{
    private static readonly Dictionary<ServiceRequestStatus, HashSet<ServiceRequestStatus>> Allowed =
        new()
        {
            [ServiceRequestStatus.Pending] =
            [
                ServiceRequestStatus.Accepted,
                ServiceRequestStatus.Assigned,
                ServiceRequestStatus.Cancelled,
                ServiceRequestStatus.Rejected
            ],
            [ServiceRequestStatus.Accepted] =
            [
                ServiceRequestStatus.Assigned,
                ServiceRequestStatus.InProgress,
                ServiceRequestStatus.Cancelled,
                ServiceRequestStatus.Rejected
            ],
            [ServiceRequestStatus.Assigned] =
            [
                ServiceRequestStatus.InProgress,
                ServiceRequestStatus.Cancelled,
                ServiceRequestStatus.Rejected
            ],
            [ServiceRequestStatus.InProgress] =
            [
                ServiceRequestStatus.Completed,
                ServiceRequestStatus.Failed,
                ServiceRequestStatus.Cancelled
            ],
            [ServiceRequestStatus.Completed] = [],
            [ServiceRequestStatus.Cancelled] = [],
            [ServiceRequestStatus.Rejected] = [],
            [ServiceRequestStatus.Failed] = []
        };

    public static bool CanTransition(ServiceRequestStatus from, ServiceRequestStatus to) =>
        Allowed.TryGetValue(from, out var next) && next.Contains(to);

    public static IReadOnlyCollection<ServiceRequestStatus> GetAllowedTransitions(ServiceRequestStatus from) =>
        Allowed.TryGetValue(from, out var next) ? next : Array.Empty<ServiceRequestStatus>();
}