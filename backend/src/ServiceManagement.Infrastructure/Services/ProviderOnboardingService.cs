using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceManagement.Application.Common.Exceptions;
using ServiceManagement.Application.Common.Storage;
using ServiceManagement.Application.Providers.Dtos;
using ServiceManagement.Application.Providers.Interfaces;
using ServiceManagement.Domain.Entities;
using ServiceManagement.Domain.Enums;
using ServiceManagement.Domain.Services;
using ServiceManagement.Infrastructure.Options;
using ServiceManagement.Infrastructure.Persistence;

namespace ServiceManagement.Infrastructure.Services;

public class ProviderOnboardingService : IProviderOnboardingService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly FileStorageOptions _storageOptions;

    public ProviderOnboardingService(
        AppDbContext db,
        IFileStorage storage,
        IOptions<FileStorageOptions> storageOptions)
    {
        _db = db;
        _storage = storage;
        _storageOptions = storageOptions.Value;
    }

    public async Task<ProviderProfileDto> GetMineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await LoadProfileByUserAsync(userId, cancellationToken);
        return await MapAsync(profile, cancellationToken);
    }

    public async Task<ProviderProfileDto> UpdateApplicationAsync(
        Guid userId,
        UpdateProviderApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await LoadProfileByUserAsync(userId, cancellationToken);

        if (profile.ApprovalStatus == ProviderApprovalStatus.Suspended)
        {
            throw new ForbiddenException("A suspended application cannot be edited. Contact an administrator.");
        }

        var serviceIds = request.ServiceIds.Distinct().ToList();
        var services = await _db.Services
            .Include(s => s.Category)
            .Where(s => serviceIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        if (services.Count != serviceIds.Count)
        {
            throw new NotFoundException("One or more selected services were not found.");
        }

        var inactive = services.FirstOrDefault(s => !s.IsActive || !s.Category.IsActive);
        if (inactive is not null)
        {
            throw new AppException($"Service '{inactive.Name}' is not currently available to apply for.");
        }

        profile.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        profile.UpdatedAt = DateTime.UtcNow;

        var existing = profile.ProviderServices.ToDictionary(ps => ps.ServiceId);
        var now = DateTime.UtcNow;

        foreach (var serviceId in serviceIds)
        {
            if (existing.TryGetValue(serviceId, out var current))
            {
                if (current.Status == ProviderServiceStatus.Rejected)
                {
                    current.Status = ProviderServiceStatus.Pending;
                    current.AppliedAt = now;
                    current.ReviewedAt = null;
                }

                continue;
            }

            profile.ProviderServices.Add(new ProviderService
            {
                ProviderProfileId = profile.Id,
                ServiceId = serviceId,
                Status = ProviderServiceStatus.Pending,
                AppliedAt = now
            });
        }

        var removed = profile.ProviderServices
            .Where(ps => !serviceIds.Contains(ps.ServiceId))
            .ToList();
        foreach (var row in removed)
        {
            profile.ProviderServices.Remove(row);
        }

        if (profile.ApprovalStatus == ProviderApprovalStatus.Rejected)
        {
            profile.ApprovalStatus = ProviderApprovalStatus.Pending;
            profile.ReviewReason = null;
            profile.ReviewedAt = null;
            profile.ReviewedByUserId = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetMineAsync(userId, cancellationToken);
    }

    public async Task<ProviderProfileDto> SetAvailabilityAsync(
        Guid userId,
        SetAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await LoadProfileByUserAsync(userId, cancellationToken);

        if (profile.ApprovalStatus != ProviderApprovalStatus.Approved || !profile.IsActive)
        {
            throw new ForbiddenException(
                "Availability can only be changed after the application is approved and the profile is active.");
        }

        profile.AvailabilityStatus = request.AvailabilityStatus;
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetMineAsync(userId, cancellationToken);
    }

    public async Task<ProviderDocumentDto> UploadDocumentAsync(
        Guid userId,
        Guid documentRequirementId,
        string originalFileName,
        string contentType,
        long fileSize,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var profile = await LoadProfileByUserAsync(userId, cancellationToken);

        if (profile.ApprovalStatus == ProviderApprovalStatus.Suspended)
        {
            throw new ForbiddenException("A suspended application cannot be updated.");
        }

        ValidateUpload(originalFileName, contentType, fileSize);

        var requirement = await _db.CategoryDocumentRequirements
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == documentRequirementId, cancellationToken)
            ?? throw new NotFoundException("Document requirement not found.");

        var existing = profile.Documents.FirstOrDefault(d => d.DocumentRequirementId == documentRequirementId);
        if (existing is not null)
        {
            await _storage.DeleteAsync(existing.StorageKey, cancellationToken);
            _db.ProviderDocuments.Remove(existing);
            profile.Documents.Remove(existing);
        }

        var document = new ProviderDocument
        {
            ProviderProfileId = profile.Id,
            DocumentRequirementId = requirement.Id,
            OriginalFileName = SanitizeFileName(originalFileName),
            ContentType = NormalizeContentType(contentType, originalFileName),
            FileSize = fileSize
        };

        var extension = Path.GetExtension(document.OriginalFileName);
        if (extension.Length > 8)
        {
            extension = string.Empty;
        }

        document.StorageKey = $"{profile.Id:N}/{document.Id:N}{extension.ToLowerInvariant()}";

        await _storage.SaveAsync(document.StorageKey, content, cancellationToken);

        _db.ProviderDocuments.Add(document);

        if (profile.ApprovalStatus == ProviderApprovalStatus.Rejected)
        {
            profile.ApprovalStatus = ProviderApprovalStatus.Pending;
            profile.ReviewReason = null;
            profile.ReviewedAt = null;
            profile.ReviewedByUserId = null;
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new ProviderDocumentDto(
            document.Id,
            requirement.Id,
            requirement.Name,
            requirement.CategoryId,
            requirement.Category.Name,
            document.OriginalFileName,
            document.ContentType,
            document.FileSize,
            document.CreatedAt);
    }

    public async Task DeleteDocumentAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var profile = await LoadProfileByUserAsync(userId, cancellationToken);
        var document = profile.Documents.FirstOrDefault(d => d.Id == documentId)
            ?? throw new NotFoundException("Document not found.");

        if (profile.ApprovalStatus is ProviderApprovalStatus.Approved or ProviderApprovalStatus.Suspended)
        {
            throw new ForbiddenException("Approved documents can only be changed by an administrator after review.");
        }

        await _storage.DeleteAsync(document.StorageKey, cancellationToken);
        _db.ProviderDocuments.Remove(document);
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProviderDocumentFile> OpenDocumentAsync(
        Guid requesterUserId,
        bool canManageProviders,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _db.ProviderDocuments
            .Include(d => d.ProviderProfile)
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken)
            ?? throw new NotFoundException("Document not found.");

        if (!canManageProviders && document.ProviderProfile.UserId != requesterUserId)
        {
            throw new ForbiddenException();
        }

        var stream = await _storage.OpenReadAsync(document.StorageKey, cancellationToken);
        return new ProviderDocumentFile(stream, document.ContentType, document.OriginalFileName);
    }

    public async Task<IReadOnlyList<ProviderListItemDto>> ListAsync(
        ProviderListQuery query,
        CancellationToken cancellationToken = default)
    {
        var profiles = _db.ServiceProviderProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.ProviderServices).ThenInclude(ps => ps.Service)
            .Include(p => p.Documents)
            .AsQueryable();

        if (query.ApprovalStatus.HasValue)
        {
            profiles = profiles.Where(p => p.ApprovalStatus == query.ApprovalStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            profiles = profiles.Where(p =>
                p.User.FirstName.ToLower().Contains(term)
                || p.User.LastName.ToLower().Contains(term)
                || (p.User.Email != null && p.User.Email.ToLower().Contains(term)));
        }

        var list = await profiles
            .OrderBy(p => p.ApprovalStatus)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var categoryIds = list
            .SelectMany(p => p.ProviderServices.Select(ps => ps.Service.CategoryId))
            .Distinct()
            .ToList();

        var required = await _db.CategoryDocumentRequirements
            .AsNoTracking()
            .Where(r => categoryIds.Contains(r.CategoryId) && r.IsRequired)
            .Select(r => new { r.Id, r.CategoryId })
            .ToListAsync(cancellationToken);

        return list.Select(p =>
        {
            var appliedCategories = p.ProviderServices.Select(ps => ps.Service.CategoryId).ToHashSet();
            var uploaded = p.Documents.Select(d => d.DocumentRequirementId).ToHashSet();
            var missing = required.Count(r => appliedCategories.Contains(r.CategoryId) && !uploaded.Contains(r.Id));

            return new ProviderListItemDto(
                p.Id,
                p.UserId,
                p.User.FirstName,
                p.User.LastName,
                p.User.Email ?? string.Empty,
                p.User.PhoneNumber,
                p.ApprovalStatus,
                p.IsActive,
                p.AvailabilityStatus,
                p.ProviderServices.Select(ps => ps.Service.Name).OrderBy(n => n).ToList(),
                p.Documents.Count,
                missing > 0,
                p.CreatedAt,
                p.ReviewedAt);
        }).ToList();
    }

    public async Task<ProviderProfileDto> GetByIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var profile = await LoadProfileAsync(profileId, cancellationToken);
        return await MapAsync(profile, cancellationToken);
    }

    public async Task<ProviderProfileDto> ApproveAsync(
        Guid profileId,
        Guid adminUserId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var profile = await LoadProfileAsync(profileId, cancellationToken);

        if (profile.ApprovalStatus == ProviderApprovalStatus.Approved)
        {
            throw new ConflictException("This provider is already approved.");
        }

        var now = DateTime.UtcNow;
        profile.ApprovalStatus = ProviderApprovalStatus.Approved;
        profile.ReviewReason = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        profile.ReviewedAt = now;
        profile.ReviewedByUserId = adminUserId;
        profile.IsActive = true;
        profile.UpdatedAt = now;

        foreach (var service in profile.ProviderServices.Where(ps => ps.Status == ProviderServiceStatus.Pending))
        {
            service.Status = ProviderServiceStatus.Approved;
            service.ReviewedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(profile, cancellationToken);
    }

    public async Task<ProviderProfileDto> RejectAsync(
        Guid profileId,
        Guid adminUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var profile = await LoadProfileAsync(profileId, cancellationToken);

        if (profile.ApprovalStatus == ProviderApprovalStatus.Rejected)
        {
            throw new ConflictException("This provider is already rejected.");
        }

        var now = DateTime.UtcNow;
        profile.ApprovalStatus = ProviderApprovalStatus.Rejected;
        profile.ReviewReason = reason.Trim();
        profile.ReviewedAt = now;
        profile.ReviewedByUserId = adminUserId;
        profile.AvailabilityStatus = AvailabilityStatus.Unavailable;
        profile.UpdatedAt = now;

        foreach (var service in profile.ProviderServices.Where(ps => ps.Status == ProviderServiceStatus.Pending))
        {
            service.Status = ProviderServiceStatus.Rejected;
            service.ReviewedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(profile, cancellationToken);
    }

    public async Task<ProviderProfileDto> SuspendAsync(
        Guid profileId,
        Guid adminUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var profile = await LoadProfileAsync(profileId, cancellationToken);

        if (profile.ApprovalStatus != ProviderApprovalStatus.Approved)
        {
            throw new AppException("Only an approved provider can be suspended.");
        }

        var now = DateTime.UtcNow;
        profile.ApprovalStatus = ProviderApprovalStatus.Suspended;
        profile.ReviewReason = reason.Trim();
        profile.ReviewedAt = now;
        profile.ReviewedByUserId = adminUserId;
        profile.AvailabilityStatus = AvailabilityStatus.Unavailable;
        profile.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(profile, cancellationToken);
    }

    public async Task<ProviderProfileDto> ReinstateAsync(
        Guid profileId,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var profile = await LoadProfileAsync(profileId, cancellationToken);

        if (profile.ApprovalStatus != ProviderApprovalStatus.Suspended)
        {
            throw new AppException("Only a suspended provider can be reinstated.");
        }

        var now = DateTime.UtcNow;
        profile.ApprovalStatus = ProviderApprovalStatus.Approved;
        profile.ReviewReason = null;
        profile.ReviewedAt = now;
        profile.ReviewedByUserId = adminUserId;
        profile.IsActive = true;
        profile.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(profile, cancellationToken);
    }

    public async Task<ProviderProfileDto> SetActiveAsync(
        Guid profileId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var profile = await LoadProfileAsync(profileId, cancellationToken);
        profile.IsActive = isActive;
        if (!isActive)
        {
            profile.AvailabilityStatus = AvailabilityStatus.Unavailable;
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(profile, cancellationToken);
    }

    public async Task<bool> IsEligibleForServiceAsync(
        Guid providerProfileId,
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.ServiceProviderProfiles
            .AsNoTracking()
            .Include(p => p.ProviderServices)
            .FirstOrDefaultAsync(p => p.Id == providerProfileId, cancellationToken)
            ?? throw new NotFoundException("Provider not found.");

        return ProviderEligibility.CanReceiveRequests(profile, serviceId);
    }

    private async Task<ServiceProviderProfile> LoadProfileByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await QueryProfiles()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Provider profile not found.");
    }

    private async Task<ServiceProviderProfile> LoadProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        return await QueryProfiles()
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken)
            ?? throw new NotFoundException("Provider not found.");
    }

    private IQueryable<ServiceProviderProfile> QueryProfiles() =>
        _db.ServiceProviderProfiles
            .Include(p => p.User)
            .Include(p => p.ProviderServices).ThenInclude(ps => ps.Service).ThenInclude(s => s.Category)
            .Include(p => p.Documents).ThenInclude(d => d.DocumentRequirement).ThenInclude(r => r.Category);

    private async Task<ProviderProfileDto> MapAsync(ServiceProviderProfile profile, CancellationToken cancellationToken)
    {
        var missing = await GetMissingRequirementsAsync(profile, cancellationToken);

        return new ProviderProfileDto(
            profile.Id,
            profile.UserId,
            profile.User.FirstName,
            profile.User.LastName,
            profile.User.Email ?? string.Empty,
            profile.User.PhoneNumber,
            profile.Description,
            profile.ApprovalStatus,
            profile.IsActive,
            profile.AvailabilityStatus,
            profile.Rating,
            profile.ReviewReason,
            profile.ReviewedAt,
            profile.CreatedAt,
            profile.UpdatedAt,
            profile.ProviderServices
                .OrderBy(ps => ps.Service.Category.Name)
                .ThenBy(ps => ps.Service.Name)
                .Select(ps => new ProviderServiceItemDto(
                    ps.ServiceId,
                    ps.Service.Name,
                    ps.Service.CategoryId,
                    ps.Service.Category.Name,
                    ps.Service.BasePrice,
                    ps.Status,
                    ps.AppliedAt,
                    ps.ReviewedAt))
                .ToList(),
            profile.Documents
                .OrderBy(d => d.DocumentRequirement.Name)
                .Select(d => new ProviderDocumentDto(
                    d.Id,
                    d.DocumentRequirementId,
                    d.DocumentRequirement.Name,
                    d.DocumentRequirement.CategoryId,
                    d.DocumentRequirement.Category.Name,
                    d.OriginalFileName,
                    d.ContentType,
                    d.FileSize,
                    d.CreatedAt))
                .ToList(),
            missing);
    }

    private async Task<IReadOnlyList<MissingDocumentRequirementDto>> GetMissingRequirementsAsync(
        ServiceProviderProfile profile,
        CancellationToken cancellationToken)
    {
        var categoryIds = profile.ProviderServices
            .Select(ps => ps.Service.CategoryId)
            .Distinct()
            .ToList();

        if (categoryIds.Count == 0)
        {
            return [];
        }

        var uploaded = profile.Documents.Select(d => d.DocumentRequirementId).ToHashSet();

        return await _db.CategoryDocumentRequirements
            .AsNoTracking()
            .Where(r => categoryIds.Contains(r.CategoryId) && r.IsRequired && !uploaded.Contains(r.Id))
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .Select(r => new MissingDocumentRequirementDto(
                r.Id,
                r.CategoryId,
                r.Category.Name,
                r.Name,
                r.Description))
            .ToListAsync(cancellationToken);
    }

    private void ValidateUpload(string originalFileName, string contentType, long fileSize)
    {
        if (fileSize <= 0)
        {
            throw new AppException("The uploaded file is empty.");
        }

        if (fileSize > _storageOptions.MaxFileBytes)
        {
            throw new AppException($"File is too large. Maximum size is {_storageOptions.MaxFileBytes / (1024 * 1024)} MB.");
        }

        var extension = Path.GetExtension(originalFileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new AppException("Unsupported file type. Upload a PDF, JPEG, PNG, or WebP file.");
        }

        var normalized = NormalizeContentType(contentType, originalFileName);
        var allowed = _storageOptions.AllowedContentTypes
            .Select(t => t.ToLowerInvariant())
            .ToHashSet();
        if (!allowed.Contains(normalized))
        {
            throw new AppException("Unsupported file type. Upload a PDF, JPEG, PNG, or WebP file.");
        }
    }

    private static string NormalizeContentType(string contentType, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType) && !contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return contentType.Split(';')[0].Trim().ToLowerInvariant();
        }

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(name))
        {
            return "document";
        }

        return name.Length <= 260 ? name : name[..260];
    }
}
