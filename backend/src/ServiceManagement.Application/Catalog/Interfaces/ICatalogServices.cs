using ServiceManagement.Application.Catalog.Dtos;

namespace ServiceManagement.Application.Catalog.Interfaces;

public interface IServiceCategoryService
{
    Task<IReadOnlyList<ServiceCategoryDto>> GetAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<ServiceCategoryDto> GetByIdAsync(Guid id, bool includeInactive, CancellationToken cancellationToken = default);
    Task<ServiceCategoryDto> CreateAsync(CreateServiceCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceCategoryDto> UpdateAsync(Guid id, UpdateServiceCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-disables a category. Categories are never hard-deleted while services reference them.
    /// </summary>
    Task DisableAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IServiceCatalogService
{
    Task<IReadOnlyList<ServiceDto>> GetAsync(CatalogQuery query, CancellationToken cancellationToken = default);
    Task<ServiceDto> GetByIdAsync(Guid id, bool includeInactive, CancellationToken cancellationToken = default);
    Task<ServiceDto> CreateAsync(CreateServiceRequest request, CancellationToken cancellationToken = default);
    Task<ServiceDto> UpdateAsync(Guid id, UpdateServiceRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
