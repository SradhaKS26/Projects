using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagement.Application.Catalog.Dtos;
using ServiceManagement.Application.Catalog.Interfaces;
using ServiceManagement.Application.Common.Models;
using ServiceManagement.Domain.Constants;
using ServiceManagement.Infrastructure.Auth;

namespace ServiceManagement.Api.Controllers;

[ApiController]
[Route("api/document-requirements")]
[Authorize]
public class DocumentRequirementsController : ControllerBase
{
    private readonly IDocumentRequirementService _documentRequirements;

    public DocumentRequirementsController(IDocumentRequirementService documentRequirements)
    {
        _documentRequirements = documentRequirements;
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageServices)]
    public async Task<ActionResult<ApiResponse<CategoryDocumentRequirementDto>>> Update(
        Guid id,
        [FromBody] UpdateDocumentRequirementRequest request,
        CancellationToken cancellationToken)
    {
        var requirement = await _documentRequirements.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CategoryDocumentRequirementDto>.Ok(requirement, "Document requirement updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageServices)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _documentRequirements.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Document requirement removed."));
    }
}
