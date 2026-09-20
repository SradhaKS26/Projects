using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagement.Application.Catalog.Dtos;
using ServiceManagement.Application.Catalog.Interfaces;
using ServiceManagement.Application.Common.Models;
using ServiceManagement.Domain.Constants;
using ServiceManagement.Infrastructure.Auth;

namespace ServiceManagement.Api.Controllers;

[ApiController]
[Route("api/services")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly IServiceCatalogService _catalogService;

    public ServicesController(IServiceCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>
    /// Browses the service catalog. Customers and providers see only active services
    /// in active categories; administrators may include inactive records.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ServiceDto>>>> Get(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = new CatalogQuery(categoryId, search, includeInactive && CanManage());
        var services = await _catalogService.GetAsync(query, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ServiceDto>>.Ok(services));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var service = await _catalogService.GetByIdAsync(id, CanManage(), cancellationToken);
        return Ok(ApiResponse<ServiceDto>.Ok(service));
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageServices)]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> Create(
        [FromBody] CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var service = await _catalogService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = service.Id },
            ApiResponse<ServiceDto>.Ok(service, "Service created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageServices)]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> Update(
        Guid id,
        [FromBody] UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var service = await _catalogService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ServiceDto>.Ok(service, "Service updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageServices)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _catalogService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Service removed."));
    }

    private bool CanManage() =>
        User.HasClaim(PermissionPolicies.ClaimType, Permissions.ManageServices);
}
