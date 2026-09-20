using Microsoft.EntityFrameworkCore;
using ServiceManagement.Application.Catalog.Dtos;
using ServiceManagement.Application.Catalog.Interfaces;
using ServiceManagement.Application.Common.Exceptions;
using ServiceManagement.Infrastructure.Persistence;
using DomainService = ServiceManagement.Domain.Entities.Service;

namespace ServiceManagement.Infrastructure.Services;

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly AppDbContext _db;

    public ServiceCatalogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ServiceDto>> GetAsync(
        CatalogQuery query,
        CancellationToken cancellationToken = default)
    {
        var services = _db.Services.AsNoTracking().Include(s => s.Category).AsQueryable();

        if (!query.IncludeInactive)
        {
            // An inactive category hides its services from customers even if the
            // service row itself is still marked active.
            services = services.Where(s => s.IsActive && s.Category.IsActive);
        }

        if (query.CategoryId.HasValue)
        {
            services = services.Where(s => s.CategoryId == query.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            services = services.Where(s =>
                s.Name.ToLower().Contains(term) ||
                (s.Description != null && s.Description.ToLower().Contains(term)));
        }

        return await services
            .OrderBy(s => s.Category.Name)
            .ThenBy(s => s.Name)
            .Select(s => new ServiceDto(
                s.Id,
                s.CategoryId,
                s.Category.Name,
                s.Name,
                s.Description,
                s.BasePrice,
                s.IsActive,
                s.EstimatedDurationMinutes,
                s.CreatedAt,
                s.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceDto> GetByIdAsync(
        Guid id,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Services
            .AsNoTracking()
            .Include(s => s.Category)
            .Where(s => s.Id == id);

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive && s.Category.IsActive);
        }

        return await query
            .Select(s => new ServiceDto(
                s.Id,
                s.CategoryId,
                s.Category.Name,
                s.Name,
                s.Description,
                s.BasePrice,
                s.IsActive,
                s.EstimatedDurationMinutes,
                s.CreatedAt,
                s.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Service not found.");
    }

    public async Task<ServiceDto> CreateAsync(
        CreateServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.ServiceCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Service category not found.");

        var name = request.Name.Trim();
        await EnsureNameIsAvailableAsync(request.CategoryId, name, null, cancellationToken);

        var service = new DomainService
        {
            CategoryId = category.Id,
            Name = name,
            Description = request.Description?.Trim(),
            BasePrice = request.BasePrice,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            IsActive = request.IsActive
        };

        _db.Services.Add(service);
        await _db.SaveChangesAsync(cancellationToken);

        return Map(service, category.Name);
    }

    public async Task<ServiceDto> UpdateAsync(
        Guid id,
        UpdateServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = await _db.Services
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Service not found.");

        var category = await _db.ServiceCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Service category not found.");

        var name = request.Name.Trim();
        await EnsureNameIsAvailableAsync(request.CategoryId, name, id, cancellationToken);

        service.CategoryId = category.Id;
        service.Name = name;
        service.Description = request.Description?.Trim();
        service.BasePrice = request.BasePrice;
        service.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
        service.IsActive = request.IsActive;
        service.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Map(service, category.Name);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await _db.Services
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Service not found.");

        // Historical requests must stay readable, so a service that has ever been
        // booked is deactivated rather than removed.
        var hasRequests = await _db.ServiceRequests.AnyAsync(r => r.ServiceId == id, cancellationToken);
        if (hasRequests)
        {
            service.IsActive = false;
            service.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.Services.Remove(service);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNameIsAvailableAsync(
        Guid categoryId,
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var exists = await _db.Services.AnyAsync(
            s => s.CategoryId == categoryId
                && s.Name.ToLower() == name.ToLower()
                && (excludeId == null || s.Id != excludeId),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException($"A service named '{name}' already exists in this category.");
        }
    }

    private static ServiceDto Map(DomainService service, string categoryName) => new(
        service.Id,
        service.CategoryId,
        categoryName,
        service.Name,
        service.Description,
        service.BasePrice,
        service.IsActive,
        service.EstimatedDurationMinutes,
        service.CreatedAt,
        service.UpdatedAt);
}
