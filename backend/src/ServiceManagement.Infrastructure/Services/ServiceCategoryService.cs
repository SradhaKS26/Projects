using Microsoft.EntityFrameworkCore;
using ServiceManagement.Application.Catalog.Dtos;
using ServiceManagement.Application.Catalog.Interfaces;
using ServiceManagement.Application.Common.Exceptions;
using ServiceManagement.Domain.Entities;
using ServiceManagement.Infrastructure.Persistence;

namespace ServiceManagement.Infrastructure.Services;

public class ServiceCategoryService : IServiceCategoryService
{
    private readonly AppDbContext _db;

    public ServiceCategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ServiceCategoryDto>> GetAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ServiceCategories.AsNoTracking().OrderBy(c => c.Name).AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);

            return await query
                .Select(c => new ServiceCategoryDto(
                    c.Id,
                    c.Name,
                    c.Description,
                    c.ImageUrl,
                    c.IsActive,
                    c.Services.Count(s => s.IsActive),
                    c.CreatedAt,
                    c.UpdatedAt))
                .ToListAsync(cancellationToken);
        }

        return await query
            .Select(c => new ServiceCategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.ImageUrl,
                c.IsActive,
                c.Services.Count,
                c.CreatedAt,
                c.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceCategoryDto> GetByIdAsync(
        Guid id,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new
            {
                Category = c,
                TotalServices = c.Services.Count,
                ActiveServices = c.Services.Count(s => s.IsActive)
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Service category not found.");

        if (!includeInactive && !category.Category.IsActive)
        {
            throw new NotFoundException("Service category not found.");
        }

        return Map(
            category.Category,
            includeInactive ? category.TotalServices : category.ActiveServices);
    }

    public async Task<ServiceCategoryDto> CreateAsync(
        CreateServiceCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        await EnsureNameIsAvailableAsync(name, null, cancellationToken);

        var category = new ServiceCategory
        {
            Name = name,
            Description = request.Description?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            IsActive = request.IsActive
        };

        _db.ServiceCategories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);

        return Map(category, 0);
    }

    public async Task<ServiceCategoryDto> UpdateAsync(
        Guid id,
        UpdateServiceCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.ServiceCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Service category not found.");

        var name = request.Name.Trim();
        await EnsureNameIsAvailableAsync(name, id, cancellationToken);

        category.Name = name;
        category.Description = request.Description?.Trim();
        category.ImageUrl = request.ImageUrl?.Trim();
        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        var serviceCount = await _db.Services.CountAsync(s => s.CategoryId == id, cancellationToken);
        return Map(category, serviceCount);
    }

    public async Task DisableAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _db.ServiceCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Service category not found.");

        // Deactivating a category must not silently leave bookable services behind,
        // so the services under it are deactivated in the same transaction.
        var services = await _db.Services
            .Where(s => s.CategoryId == id && s.IsActive)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var service in services)
        {
            service.IsActive = false;
            service.UpdatedAt = now;
        }

        category.IsActive = false;
        category.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNameIsAvailableAsync(string name, Guid? excludeId, CancellationToken cancellationToken)
    {
        var exists = await _db.ServiceCategories
            .AnyAsync(c => c.Name.ToLower() == name.ToLower() && (excludeId == null || c.Id != excludeId), cancellationToken);

        if (exists)
        {
            throw new ConflictException($"A service category named '{name}' already exists.");
        }
    }

    private static ServiceCategoryDto Map(ServiceCategory category, int serviceCount) => new(
        category.Id,
        category.Name,
        category.Description,
        category.ImageUrl,
        category.IsActive,
        serviceCount,
        category.CreatedAt,
        category.UpdatedAt);
}
