using ServiceManagement.Application.Providers.Dtos;

namespace ServiceManagement.Application.Providers.Interfaces;

public interface IProviderOnboardingService
{
    Task<ProviderProfileDto> GetMineAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ProviderProfileDto> UpdateApplicationAsync(
        Guid userId,
        UpdateProviderApplicationRequest request,
        CancellationToken cancellationToken = default);

    Task<ProviderProfileDto> SetAvailabilityAsync(
        Guid userId,
        SetAvailabilityRequest request,
        CancellationToken cancellationToken = default);

    Task<ProviderDocumentDto> UploadDocumentAsync(
        Guid userId,
        Guid documentRequirementId,
        string originalFileName,
        string contentType,
        long fileSize,
        Stream content,
        CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(Guid userId, Guid documentId, CancellationToken cancellationToken = default);

    Task<ProviderDocumentFile> OpenDocumentAsync(
        Guid requesterUserId,
        bool canManageProviders,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProviderListItemDto>> ListAsync(
        ProviderListQuery query,
        CancellationToken cancellationToken = default);

    Task<ProviderProfileDto> GetByIdAsync(Guid profileId, CancellationToken cancellationToken = default);

    Task<ProviderProfileDto> ApproveAsync(
        Guid profileId,
        Guid adminUserId,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<ProviderProfileDto> RejectAsync(
        Guid profileId,
        Guid adminUserId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<ProviderProfileDto> SuspendAsync(
        Guid profileId,
        Guid adminUserId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<ProviderProfileDto> ReinstateAsync(
        Guid profileId,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    Task<ProviderProfileDto> SetActiveAsync(
        Guid profileId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<bool> IsEligibleForServiceAsync(
        Guid providerProfileId,
        Guid serviceId,
        CancellationToken cancellationToken = default);
}

public record ProviderDocumentFile(Stream Content, string ContentType, string FileName);
