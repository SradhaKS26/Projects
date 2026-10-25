using Microsoft.EntityFrameworkCore;
using ServiceManagement.Application.Common.Exceptions;
using ServiceManagement.Application.Requests.Dtos;
using ServiceManagement.Application.Requests.Interfaces;
using ServiceManagement.Application.Reviews.Dtos;
using ServiceManagement.Application.Reviews.Interfaces;
using ServiceManagement.Domain.Entities;
using ServiceManagement.Domain.Services;
using ServiceManagement.Infrastructure.Persistence;

namespace ServiceManagement.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly IServiceRequestService _requests;

    public ReviewService(AppDbContext db, IClock clock, IServiceRequestService requests)
    {
        _db = db;
        _clock = clock;
        _requests = requests;
    }

    public async Task<ServiceRequestDto> SubmitAsync(
        Guid customerUserId,
        Guid requestId,
        SubmitReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var job = await _db.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new NotFoundException("Service request was not found.");

        if (job.CustomerId != customerUserId)
        {
            throw new ForbiddenException("Only the customer who placed this request can review it.");
        }

        var alreadyReviewed = await _db.Reviews.AnyAsync(r => r.ServiceRequestId == requestId, cancellationToken);
        if (!ReviewRules.CanCustomerReview(
                job.Status,
                job.CustomerId,
                customerUserId,
                job.AssignedProviderId,
                alreadyReviewed))
        {
            if (alreadyReviewed)
            {
                throw new ConflictException("This job has already been reviewed.");
            }

            throw new ConflictException("You can only review a completed job.");
        }

        var now = _clock.UtcNow;
        var providerId = job.AssignedProviderId!.Value;
        _db.Reviews.Add(new Review
        {
            ServiceRequestId = job.Id,
            CustomerId = customerUserId,
            ProviderId = providerId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedAt = now
        });

        var stars = request.Rating == 1 ? "1 star" : $"{request.Rating} stars";
        _db.UserNotifications.Add(new UserNotification
        {
            UserId = providerId,
            ServiceRequestId = job.Id,
            Title = "A customer rated your work",
            Body = $"You received {stars}. Open Requests to see the review.",
            IsRead = false,
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);
        await RecalculateProviderRatingAsync(providerId, now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return await _requests.GetAsync(
            customerUserId,
            isProvider: false,
            isCustomer: true,
            requestId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewDto>> ListReceivedAsync(
        Guid providerUserId,
        CancellationToken cancellationToken = default)
    {
        return await (
                from r in _db.Reviews.AsNoTracking()
                join req in _db.ServiceRequests.AsNoTracking() on r.ServiceRequestId equals req.Id
                join service in _db.Services.AsNoTracking() on req.ServiceId equals service.Id
                join category in _db.ServiceCategories.AsNoTracking() on service.CategoryId equals category.Id
                where r.ProviderId == providerUserId
                orderby r.CreatedAt descending
                select new ReviewDto(
                    r.Id,
                    r.ServiceRequestId,
                    r.ProviderId,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    service.Name,
                    category.Name))
            .ToListAsync(cancellationToken);
    }

    private async Task RecalculateProviderRatingAsync(
        Guid providerUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var ratings = await _db.Reviews
            .Where(r => r.ProviderId == providerUserId)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        var profile = await _db.ServiceProviderProfiles
            .FirstOrDefaultAsync(p => p.UserId == providerUserId, cancellationToken);
        if (profile is null)
        {
            return;
        }

        profile.Rating = ratings.Count == 0
            ? 0
            : Math.Round((decimal)ratings.Average(), 2, MidpointRounding.AwayFromZero);
        profile.UpdatedAt = now;
    }
}
