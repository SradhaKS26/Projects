using ServiceManagement.Domain.Enums;

namespace ServiceManagement.Domain.Services;

/// <summary>
/// After a job is Completed, the customer may leave one 1–5 rating for the assigned provider.
/// Provider-to-customer ratings are deferred.
/// </summary>
public static class ReviewRules
{
    public const int MinRating = 1;
    public const int MaxRating = 5;

    public static bool CanCustomerReview(
        ServiceRequestStatus status,
        Guid customerId,
        Guid actorId,
        Guid? assignedProviderId,
        bool alreadyReviewed)
    {
        return status == ServiceRequestStatus.Completed
            && actorId == customerId
            && assignedProviderId is not null
            && !alreadyReviewed;
    }

    public static bool IsValidRating(int rating) =>
        rating is >= MinRating and <= MaxRating;
}
