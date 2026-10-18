using ServiceManagement.Application.Requests.Dtos;

namespace ServiceManagement.Application.Requests.Interfaces;

public interface IServiceRequestService
{
    Task<ServiceRequestDto> PlaceAsync(
        Guid customerUserId,
        PlaceServiceRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequestDto>> ListMineAsync(
        Guid customerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequestDto>> ListInboxAsync(
        Guid providerUserId,
        CancellationToken cancellationToken = default);

    Task<ServiceRequestDto> GetAsync(
        Guid userId,
        bool isProvider,
        bool isCustomer,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<ServiceRequestDto> AcceptAsync(
        Guid providerUserId,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<ServiceRequestDto> CancelAsync(
        Guid customerUserId,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<int> ExpireOverdueAsync(CancellationToken cancellationToken = default);
}
