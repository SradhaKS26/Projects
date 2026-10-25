using ServiceManagement.Application.Requests.Dtos;
using ServiceManagement.Application.Reviews.Dtos;

namespace ServiceManagement.Application.Reviews.Interfaces;

public interface IReviewService
{
    Task<ServiceRequestDto> SubmitAsync(
        Guid customerUserId,
        Guid requestId,
        SubmitReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReviewDto>> ListReceivedAsync(
        Guid providerUserId,
        CancellationToken cancellationToken = default);
}
