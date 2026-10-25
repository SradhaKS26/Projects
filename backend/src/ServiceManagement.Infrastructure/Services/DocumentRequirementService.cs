using Microsoft.EntityFrameworkCore;
using ServiceManagement.Application.Catalog.Dtos;
using ServiceManagement.Application.Catalog.Interfaces;
using ServiceManagement.Application.Common.Exceptions;
using ServiceManagement.Domain.Entities;
using ServiceManagement.Infrastructure.Persistence;

namespace ServiceManagement.Infrastructure.Services;

public class DocumentRequirementService : IDocumentRequirementService
{
    private readonly AppDbContext _db;

    public DocumentRequirementService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CategoryDocumentRequirementDto>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var categoryExists = await _db.ServiceCategories
            .AnyAsync(c => c.Id == categoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException("Service category not found.");
        }

        return await _db.CategoryDocumentRequirements
            .AsNoTracking()
            .Include(r => r.ServiceScopes)
            .Where(r => r.CategoryId == categoryId)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .Select(r => new CategoryDocumentRequirementDto(
                r.Id,
                r.CategoryId,
                r.Category.Name,
                r.Name,
                r.Description,
                r.IsRequired,
                r.SortOrder,
                r.CreatedAt,
                r.UpdatedAt,
                r.ServiceScopes.Select(s => s.ServiceId).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDocumentRequirementDto> CreateAsync(
        Guid categoryId,
        CreateDocumentRequirementRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.ServiceCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken)
            ?? throw new NotFoundException("Service category not found.");

        var name = request.Name.Trim();
        await EnsureNameIsAvailableAsync(categoryId, name, null, cancellationToken);

        var requirement = new CategoryDocumentRequirement
        {
            CategoryId = categoryId,
            Name = name,
            Description = request.Description?.Trim(),
            IsRequired = request.IsRequired,
            SortOrder = request.SortOrder
        };

        await AssignScopesAsync(requirement, category.Id, request.ServiceIds, cancellationToken);
        _db.CategoryDocumentRequirements.Add(requirement);
        await _db.SaveChangesAsync(cancellationToken);

        return Map(requirement, category.Name);
    }

    public async Task<CategoryDocumentRequirementDto> UpdateAsync(
        Guid id,
        UpdateDocumentRequirementRequest request,
        CancellationToken cancellationToken = default)
    {
        var requirement = await _db.CategoryDocumentRequirements
            .Include(r => r.Category)
            .Include(r => r.ServiceScopes)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("Document requirement not found.");

        var name = request.Name.Trim();
        await EnsureNameIsAvailableAsync(requirement.CategoryId, name, id, cancellationToken);

        requirement.Name = name;
        requirement.Description = request.Description?.Trim();
        requirement.IsRequired = request.IsRequired;
        requirement.SortOrder = request.SortOrder;
        requirement.UpdatedAt = DateTime.UtcNow;
        await AssignScopesAsync(requirement, requirement.CategoryId, request.ServiceIds, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return Map(requirement, requirement.Category.Name);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var requirement = await _db.CategoryDocumentRequirements
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("Document requirement not found.");

        var inUse = await _db.ProviderDocuments.AnyAsync(d => d.DocumentRequirementId == id, cancellationToken);
        if (inUse)
        {
            throw new ConflictException(
                "This document requirement cannot be removed because providers have already uploaded files against it.");
        }

        _db.CategoryDocumentRequirements.Remove(requirement);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNameIsAvailableAsync(
        Guid categoryId,
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var exists = await _db.CategoryDocumentRequirements.AnyAsync(
            r => r.CategoryId == categoryId
                && r.Name.ToLower() == name.ToLower()
                && (excludeId == null || r.Id != excludeId),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException($"A document requirement named '{name}' already exists in this category.");
        }
    }

    private async Task AssignScopesAsync(
        CategoryDocumentRequirement requirement,
        Guid categoryId,
        IReadOnlyList<Guid>? serviceIds,
        CancellationToken cancellationToken)
    {
        var desired = (serviceIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (desired.Count > 0)
        {
            var validCount = await _db.Services.CountAsync(
                s => s.CategoryId == categoryId && desired.Contains(s.Id),
                cancellationToken);
            if (validCount != desired.Count)
            {
                throw new AppException("Document requirement services must belong to this category.");
            }
        }

        requirement.ServiceScopes.Clear();
        foreach (var serviceId in desired)
        {
            requirement.ServiceScopes.Add(new DocumentRequirementScope
            {
                DocumentRequirement = requirement,
                ServiceId = serviceId
            });
        }
    }

    private static CategoryDocumentRequirementDto Map(CategoryDocumentRequirement requirement, string categoryName) =>
        new(
            requirement.Id,
            requirement.CategoryId,
            categoryName,
            requirement.Name,
            requirement.Description,
            requirement.IsRequired,
            requirement.SortOrder,
            requirement.CreatedAt,
            requirement.UpdatedAt,
            requirement.ServiceScopes.Select(s => s.ServiceId).ToList());
}
