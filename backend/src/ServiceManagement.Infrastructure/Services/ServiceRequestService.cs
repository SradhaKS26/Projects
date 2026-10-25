using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceManagement.Application.Common.Exceptions;
using ServiceManagement.Application.Requests.Dtos;
using ServiceManagement.Application.Requests.Interfaces;
using ServiceManagement.Application.Reviews.Dtos;
using ServiceManagement.Domain.Entities;
using ServiceManagement.Domain.Enums;
using ServiceManagement.Domain.Services;
using ServiceManagement.Infrastructure.Options;
using ServiceManagement.Infrastructure.Persistence;

namespace ServiceManagement.Infrastructure.Services;

public class ServiceRequestService : IServiceRequestService
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly RequestBroadcastOptions _options;

    public ServiceRequestService(
        AppDbContext db,
        IClock clock,
        IOptions<RequestBroadcastOptions> options)
    {
        _db = db;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<ServiceRequestDto> PlaceAsync(
        Guid customerUserId,
        PlaceServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = await _db.Services
            .Include(s => s.Category)
            .FirstOrDefaultAsync(
                s => s.Id == request.ServiceId && s.IsActive && s.Category.IsActive,
                cancellationToken)
            ?? throw new NotFoundException("Service was not found or is not available.");

        var now = _clock.UtcNow;
        var address = new CustomerAddress
        {
            UserId = customerUserId,
            AddressLine1 = request.AddressLine1.Trim(),
            AddressLine2 = string.IsNullOrWhiteSpace(request.AddressLine2) ? null : request.AddressLine2.Trim(),
            City = request.City.Trim(),
            State = request.State.Trim(),
            PostalCode = request.PostalCode.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsDefault = false
        };

        var entity = new ServiceRequest
        {
            CustomerId = customerUserId,
            ServiceId = service.Id,
            Status = ServiceRequestStatus.Pending,
            RequestedDate = now,
            ExpiresAt = now.AddMinutes(Math.Max(1, _options.ExpiryMinutes)),
            Address = address,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Price = service.BasePrice
        };

        _db.ServiceRequests.Add(entity);
        AppendHistory(
            entity.Id,
            customerUserId,
            oldStatus: null,
            newStatus: ServiceRequestStatus.Pending,
            changedAt: now,
            notes: "Request placed.");
        await _db.SaveChangesAsync(cancellationToken);

        return await GetMappedAsync(entity.Id, includeCustomer: true, includeProvider: false, includeAddress: true, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceRequestDto>> ListMineAsync(
        Guid customerUserId,
        CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);

        var ids = await _db.ServiceRequests
            .AsNoTracking()
            .Where(r => r.CustomerId == customerUserId)
            .OrderByDescending(r => r.RequestedDate)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var results = new List<ServiceRequestDto>(ids.Count);
        foreach (var id in ids)
        {
            results.Add(await GetMappedAsync(id, includeCustomer: true, includeProvider: true, includeAddress: true, cancellationToken));
        }

        return results;
    }

    public async Task<IReadOnlyList<ServiceRequestDto>> ListInboxAsync(
        Guid providerUserId,
        CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);

        var profile = await LoadProviderProfileAsync(providerUserId, cancellationToken);
        if (profile is null)
        {
            return [];
        }

        var eligibleServiceIds = profile.ProviderServices
            .Where(ps => ps.Status == ProviderServiceStatus.Approved)
            .Select(ps => ps.ServiceId)
            .ToHashSet();

        if (eligibleServiceIds.Count == 0
            || !ProviderEligibility.CanReceiveRequests(
                profile.ApprovalStatus,
                profile.IsActive,
                profile.AvailabilityStatus,
                isRegisteredForService: true))
        {
            return [];
        }

        var now = _clock.UtcNow;
        var ids = await _db.ServiceRequests
            .AsNoTracking()
            .Where(r =>
                r.Status == ServiceRequestStatus.Pending
                && r.AssignedProviderId == null
                && r.ExpiresAt > now
                && eligibleServiceIds.Contains(r.ServiceId)
                && r.CustomerId != providerUserId)
            .OrderBy(r => r.ExpiresAt)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var inbox = new List<ServiceRequestDto>(ids.Count);
        foreach (var id in ids)
        {
            inbox.Add(await GetMappedAsync(
                id,
                includeCustomer: false,
                includeProvider: false,
                includeAddress: false,
                cancellationToken));
        }

        return inbox;
    }

    public async Task<IReadOnlyList<ServiceRequestDto>> ListAssignedAsync(
        Guid providerUserId,
        CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);

        var ids = await _db.ServiceRequests
            .AsNoTracking()
            .Where(r => r.AssignedProviderId == providerUserId)
            .OrderByDescending(r => r.UpdatedAt ?? r.RequestedDate)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var results = new List<ServiceRequestDto>(ids.Count);
        foreach (var id in ids)
        {
            results.Add(await GetMappedAsync(
                id,
                includeCustomer: true,
                includeProvider: true,
                includeAddress: true,
                cancellationToken));
        }

        return results;
    }

    public async Task<ServiceRequestDto> GetAsync(
        Guid userId,
        bool isProvider,
        bool isCustomer,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);

        var request = await _db.ServiceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new NotFoundException("Service request was not found.");

        if (request.CustomerId == userId)
        {
            return await GetMappedAsync(requestId, includeCustomer: true, includeProvider: true, includeAddress: true, cancellationToken);
        }

        if (request.AssignedProviderId == userId)
        {
            return await GetMappedAsync(requestId, includeCustomer: true, includeProvider: true, includeAddress: true, cancellationToken);
        }

        if (isProvider
            && request.Status == ServiceRequestStatus.Pending
            && request.AssignedProviderId is null
            && request.ExpiresAt > _clock.UtcNow)
        {
            var profile = await LoadProviderProfileAsync(userId, cancellationToken);
            if (profile is not null && ProviderEligibility.CanReceiveRequests(profile, request.ServiceId))
            {
                return await GetMappedAsync(
                    requestId,
                    includeCustomer: false,
                    includeProvider: false,
                    includeAddress: false,
                    cancellationToken);
            }
        }

        throw new NotFoundException("Service request was not found.");
    }

    public async Task<ServiceRequestDto> AcceptAsync(
        Guid providerUserId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);

        var profile = await LoadProviderProfileAsync(providerUserId, cancellationToken)
            ?? throw new ForbiddenException("Only approved providers can accept requests.");

        var now = _clock.UtcNow;
        var snapshot = await _db.ServiceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new NotFoundException("Service request was not found.");

        if (snapshot.CustomerId == providerUserId)
        {
            throw new ForbiddenException("You cannot accept your own request.");
        }

        if (!ProviderEligibility.CanReceiveRequests(profile, snapshot.ServiceId))
        {
            throw new ForbiddenException("You are not eligible to accept this request.");
        }

        if (snapshot.Status != ServiceRequestStatus.Pending
            || snapshot.AssignedProviderId is not null)
        {
            throw new ConflictException("Another provider already accepted this request.");
        }

        if (snapshot.ExpiresAt <= now)
        {
            throw new ConflictException("This request has expired.");
        }

        if (!ServiceRequestStateMachine.CanTransition(snapshot.Status, ServiceRequestStatus.Accepted))
        {
            throw new ConflictException("This request cannot be accepted.");
        }

        var claimed = await TryClaimAsync(requestId, providerUserId, now, cancellationToken);
        if (claimed == 0)
        {
            var current = await _db.ServiceRequests.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
            if (current is null)
            {
                throw new NotFoundException("Service request was not found.");
            }

            if (current.Status != ServiceRequestStatus.Pending || current.AssignedProviderId is not null)
            {
                throw new ConflictException("Another provider already accepted this request.");
            }

            throw new ConflictException("This request has expired.");
        }

        AppendHistory(
            requestId,
            providerUserId,
            oldStatus: ServiceRequestStatus.Pending,
            newStatus: ServiceRequestStatus.Accepted,
            changedAt: now,
            notes: "Accepted by provider.");

        if (profile.AvailabilityStatus == AvailabilityStatus.Available)
        {
            profile.AvailabilityStatus = AvailabilityStatus.Busy;
            profile.UpdatedAt = now;
        }

        Notify(
            snapshot.CustomerId,
            "A provider accepted your request",
            "Open Requests to see contact details and track the job.",
            requestId,
            now);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetMappedAsync(requestId, includeCustomer: true, includeProvider: true, includeAddress: true, cancellationToken);
    }

    public async Task<ServiceRequestDto> CancelAsync(
        Guid customerUserId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);

        var request = await _db.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new NotFoundException("Service request was not found.");

        if (request.CustomerId != customerUserId)
        {
            throw new ForbiddenException();
        }

        if (!ServiceRequestStateMachine.CanTransition(request.Status, ServiceRequestStatus.Cancelled))
        {
            throw new ConflictException("This request can no longer be cancelled.");
        }

        var now = _clock.UtcNow;
        var old = request.Status;
        var providerId = request.AssignedProviderId;
        request.Status = ServiceRequestStatus.Cancelled;
        request.UpdatedAt = now;
        AppendHistory(
            request.Id,
            customerUserId,
            oldStatus: old,
            newStatus: ServiceRequestStatus.Cancelled,
            changedAt: now,
            notes: "Cancelled by customer.");

        if (providerId is Guid assignedProviderId)
        {
            Notify(
                assignedProviderId,
                "A customer cancelled a job",
                "The request you accepted was cancelled.",
                request.Id,
                now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        if (providerId is Guid idleProviderId)
        {
            await ReleaseIfIdleAsync(idleProviderId, cancellationToken);
        }

        return await GetMappedAsync(requestId, includeCustomer: true, includeProvider: true, includeAddress: true, cancellationToken);
    }

    public Task<ServiceRequestDto> StartAsync(
        Guid providerUserId,
        Guid requestId,
        CancellationToken cancellationToken = default) =>
        TransitionAssignedAsync(
            providerUserId,
            requestId,
            ServiceRequestStatus.InProgress,
            "Provider started the job.",
            "Your provider started the job",
            "Work is in progress. Pull to refresh Requests for updates.",
            cancellationToken);

    public async Task<ServiceRequestDto> CompleteAsync(
        Guid providerUserId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var completed = await TransitionAssignedAsync(
            providerUserId,
            requestId,
            ServiceRequestStatus.Completed,
            "Provider completed the job.",
            "Your request was completed",
            "The provider marked this job complete. Rate them on Requests.",
            cancellationToken,
            after: (request, now) => request.CompletedAt = now);

        await ReleaseIfIdleAsync(providerUserId, cancellationToken);
        return completed;
    }

    public async Task<ServiceRequestDto> FailAsync(
        Guid providerUserId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var failed = await TransitionAssignedAsync(
            providerUserId,
            requestId,
            ServiceRequestStatus.Failed,
            "Provider could not complete the job.",
            "Your job could not be completed",
            "The provider marked this job as failed.",
            cancellationToken);

        await ReleaseIfIdleAsync(providerUserId, cancellationToken);
        return failed;
    }

    public async Task<int> ExpireOverdueAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var overdue = await _db.ServiceRequests
            .Where(r => r.Status == ServiceRequestStatus.Pending && r.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (overdue.Count == 0)
        {
            return 0;
        }

        foreach (var request in overdue)
        {
            if (!ServiceRequestStateMachine.CanTransition(request.Status, ServiceRequestStatus.Expired))
            {
                continue;
            }

            request.Status = ServiceRequestStatus.Expired;
            request.UpdatedAt = now;
            AppendHistory(
                request.Id,
                request.CustomerId,
                oldStatus: ServiceRequestStatus.Pending,
                newStatus: ServiceRequestStatus.Expired,
                changedAt: now,
                notes: "Expired without an accept.");
            Notify(
                request.CustomerId,
                "Your request expired",
                "No provider accepted in time. Place a new request from Services.",
                request.Id,
                now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return overdue.Count;
    }

    private async Task<int> TryClaimAsync(
        Guid requestId,
        Guid providerUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // PostgreSQL: one atomic conditional UPDATE so two accepts cannot both win.
        if (_db.Database.IsRelational())
        {
            return await _db.ServiceRequests
                .Where(r =>
                    r.Id == requestId
                    && r.Status == ServiceRequestStatus.Pending
                    && r.AssignedProviderId == null
                    && r.ExpiresAt > now)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(r => r.AssignedProviderId, providerUserId)
                        .SetProperty(r => r.Status, ServiceRequestStatus.Accepted)
                        .SetProperty(r => r.UpdatedAt, now),
                    cancellationToken);
        }

        var request = await _db.ServiceRequests.FirstOrDefaultAsync(
            r => r.Id == requestId
                && r.Status == ServiceRequestStatus.Pending
                && r.AssignedProviderId == null
                && r.ExpiresAt > now,
            cancellationToken);

        if (request is null)
        {
            return 0;
        }

        request.AssignedProviderId = providerUserId;
        request.Status = ServiceRequestStatus.Accepted;
        request.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return 1;
    }

    private async Task<ServiceRequestDto> TransitionAssignedAsync(
        Guid providerUserId,
        Guid requestId,
        ServiceRequestStatus target,
        string historyNotes,
        string notificationTitle,
        string notificationBody,
        CancellationToken cancellationToken,
        Action<ServiceRequest, DateTime>? after = null)
    {
        await ExpireOverdueAsync(cancellationToken);

        var request = await _db.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new NotFoundException("Service request was not found.");

        if (request.AssignedProviderId != providerUserId)
        {
            throw new ForbiddenException("Only the assigned provider can update this job.");
        }

        if (!ServiceRequestStateMachine.CanTransition(request.Status, target))
        {
            throw new ConflictException($"This request cannot move to {target}.");
        }

        var now = _clock.UtcNow;
        var old = request.Status;
        request.Status = target;
        request.UpdatedAt = now;
        after?.Invoke(request, now);
        AppendHistory(request.Id, providerUserId, old, target, now, historyNotes);
        Notify(request.CustomerId, notificationTitle, notificationBody, request.Id, now);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetMappedAsync(
            requestId,
            includeCustomer: true,
            includeProvider: true,
            includeAddress: true,
            cancellationToken);
    }

    private async Task ReleaseIfIdleAsync(Guid providerUserId, CancellationToken cancellationToken)
    {
        var stillBusy = await _db.ServiceRequests.AnyAsync(
            r => r.AssignedProviderId == providerUserId
                && (r.Status == ServiceRequestStatus.Accepted || r.Status == ServiceRequestStatus.InProgress),
            cancellationToken);

        if (stillBusy)
        {
            return;
        }

        var profile = await LoadProviderProfileAsync(providerUserId, cancellationToken);
        if (profile is null || profile.AvailabilityStatus != AvailabilityStatus.Busy)
        {
            return;
        }

        profile.AvailabilityStatus = AvailabilityStatus.Available;
        profile.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private void Notify(
        Guid userId,
        string title,
        string body,
        Guid requestId,
        DateTime at)
    {
        _db.UserNotifications.Add(new UserNotification
        {
            UserId = userId,
            ServiceRequestId = requestId,
            Title = title,
            Body = body,
            IsRead = false,
            CreatedAt = at
        });
    }

    private void AppendHistory(
        Guid requestId,
        Guid changedByUserId,
        ServiceRequestStatus? oldStatus,
        ServiceRequestStatus newStatus,
        DateTime changedAt,
        string notes)
    {
        // Add through the DbSet so client-generated keys are Inserted, not Updated.
        // Navigation-collection Add on an already-tracked request marks the row Modified
        // (non-default Guid), which the in-memory provider then rejects.
        _db.ServiceRequestStatusHistories.Add(new ServiceRequestStatusHistory
        {
            ServiceRequestId = requestId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = changedByUserId,
            ChangedAt = changedAt,
            Notes = notes
        });
    }

    private async Task<ServiceProviderProfile?> LoadProviderProfileAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _db.ServiceProviderProfiles
            .Include(p => p.ProviderServices)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    private async Task<ServiceRequestDto> GetMappedAsync(
        Guid id,
        bool includeCustomer,
        bool includeProvider,
        bool includeAddress,
        CancellationToken cancellationToken)
    {
        var row = await _db.ServiceRequests
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new
            {
                r.Id,
                r.ServiceId,
                ServiceName = r.Service.Name,
                CategoryName = r.Service.Category.Name,
                r.Price,
                r.Status,
                r.RequestedDate,
                r.ExpiresAt,
                r.CompletedAt,
                r.Notes,
                Address = r.Address == null
                    ? null
                    : new ServiceRequestAddressDto(
                        r.Address.Id,
                        r.Address.AddressLine1,
                        r.Address.AddressLine2,
                        r.Address.City,
                        r.Address.State,
                        r.Address.PostalCode,
                        r.Address.Latitude,
                        r.Address.Longitude),
                Customer = new RequestContactDto(
                    r.Customer.Id,
                    r.Customer.FirstName,
                    r.Customer.LastName,
                    r.Customer.Email,
                    r.Customer.PhoneNumber),
                Provider = r.AssignedProvider == null
                    ? null
                    : new RequestContactDto(
                        r.AssignedProvider.Id,
                        r.AssignedProvider.FirstName,
                        r.AssignedProvider.LastName,
                        r.AssignedProvider.Email,
                        r.AssignedProvider.PhoneNumber)
            })
            .FirstAsync(cancellationToken);

        var contactsRevealed = row.Status is not ServiceRequestStatus.Pending
            and not ServiceRequestStatus.Expired;

        IReadOnlyList<RequestStatusEventDto> timeline = [];
        ReviewDto? review = null;
        decimal? providerRating = null;
        var providerReviewCount = 0;

        if (includeAddress)
        {
            timeline = await _db.ServiceRequestStatusHistories
                .AsNoTracking()
                .Where(h => h.ServiceRequestId == id)
                .OrderBy(h => h.ChangedAt)
                .Select(h => new RequestStatusEventDto(h.OldStatus, h.NewStatus, h.ChangedAt, h.Notes))
                .ToListAsync(cancellationToken);

            var reviewRow = await _db.Reviews
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ServiceRequestId == id, cancellationToken);
            if (reviewRow is not null)
            {
                review = new ReviewDto(
                    reviewRow.Id,
                    reviewRow.ServiceRequestId,
                    reviewRow.ProviderId,
                    reviewRow.Rating,
                    reviewRow.Comment,
                    reviewRow.CreatedAt,
                    row.ServiceName,
                    row.CategoryName);
            }
        }

        if (includeProvider && contactsRevealed && row.Provider is not null)
        {
            providerRating = await _db.ServiceProviderProfiles
                .AsNoTracking()
                .Where(p => p.UserId == row.Provider.UserId)
                .Select(p => (decimal?)p.Rating)
                .FirstOrDefaultAsync(cancellationToken);
            providerReviewCount = await _db.Reviews
                .CountAsync(r => r.ProviderId == row.Provider.UserId, cancellationToken);
        }

        return new ServiceRequestDto(
            row.Id,
            row.ServiceId,
            row.ServiceName,
            row.CategoryName,
            row.Price,
            row.Status,
            row.RequestedDate,
            row.ExpiresAt,
            row.CompletedAt,
            row.Notes,
            includeAddress ? row.Address : null,
            includeCustomer && contactsRevealed ? row.Customer : null,
            includeProvider && contactsRevealed ? row.Provider : null,
            providerRating,
            providerReviewCount,
            review,
            timeline);
    }
}
