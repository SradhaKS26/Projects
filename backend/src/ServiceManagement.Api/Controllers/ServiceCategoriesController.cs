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
    private readonly IDocumentRequirementService _documentRequirements;

    public ServiceCategoriesController(
        IServiceCategoryService categoryService,
        IDocumentRequirementService documentRequirements)
    {
        _categoryService = categoryService;
        _documentRequirements = documentRequirements;
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

    [HttpGet("{id:guid}/document-requirements")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDocumentRequirementDto>>>> GetDocumentRequirements(
        Guid id,
        CancellationToken cancellationToken)
    {
        var requirements = await _documentRequirements.GetByCategoryAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CategoryDocumentRequirementDto>>.Ok(requirements));
    }

    [HttpPost("{id:guid}/document-requirements")]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageServices)]
    public async Task<ActionResult<ApiResponse<CategoryDocumentRequirementDto>>> CreateDocumentRequirement(
        Guid id,
        [FromBody] CreateDocumentRequirementRequest request,
        CancellationToken cancellationToken)
    {
        var requirement = await _documentRequirements.CreateAsync(id, request, cancellationToken);
        return CreatedAtAction(
            nameof(GetDocumentRequirements),
            new { id },
            ApiResponse<CategoryDocumentRequirementDto>.Ok(requirement, "Document requirement created."));
    }

    private bool CanManage() =>
        User.HasClaim(PermissionPolicies.ClaimType, Permissions.ManageServices);
}
