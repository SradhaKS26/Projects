using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagement.Application.Catalog.Dtos;
using ServiceManagement.Application.Catalog.Interfaces;
using ServiceManagement.Application.Common.Models;
using ServiceManagement.Domain.Constants;
using ServiceManagement.Infrastructure.Auth;

namespace ServiceManagement.Api.Controllers;

[ApiController]
[Route("api/service-categories")]
[Authorize]
public class ServiceCategoriesController : ControllerBase
{
    private readonly IServiceCategoryService _categoryService;

    public ServiceCategoriesController(IServiceCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Lists categories. Inactive categories are only returned to callers holding ManageServices.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ServiceCategoryDto>>>> Get(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var effectiveIncludeInactive = includeInactive && CanManage();
        var categories = await _categoryService.GetAsync(effectiveIncludeInactive, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ServiceCategoryDto>>.Ok(categories));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ServiceCategoryDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(id, CanManage(), cancellationToken);
        return Ok(ApiResponse<ServiceCategoryDto>.Ok(category));
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageServices)]
    public async Task<ActionResult<ApiResponse<ServiceCategoryDto>>> Create(
        [FromBody] CreateServiceCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = category.Id },
            ApiResponse<ServiceCategoryDto>.Ok(category, "Service category created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageServices)]
    public async Task<ActionResult<ApiResponse<ServiceCategoryDto>>> Update(
        Guid id,
        [FromBody] UpdateServiceCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ServiceCategoryDto>.Ok(category, "Service category updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageServices)]
    public async Task<ActionResult<ApiResponse<object>>> Disable(Guid id, CancellationToken cancellationToken)
    {
        await _categoryService.DisableAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Service category deactivated."));
    }

    private bool CanManage() =>
        User.HasClaim(PermissionPolicies.ClaimType, Permissions.ManageServices);
}
