using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagement.Api.Extensions;
using ServiceManagement.Application.Common.Models;
using ServiceManagement.Application.Providers.Dtos;
using ServiceManagement.Application.Providers.Interfaces;
using ServiceManagement.Domain.Constants;
using ServiceManagement.Domain.Enums;
using ServiceManagement.Infrastructure.Auth;

namespace ServiceManagement.Api.Controllers;

[ApiController]
[Route("api/providers")]
[Authorize]
public class ProvidersController : ControllerBase
{
    private readonly IProviderOnboardingService _providers;

    public ProvidersController(IProviderOnboardingService providers)
    {
        _providers = providers;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<ProviderProfileDto>>> GetMine(CancellationToken cancellationToken)
    {
        var profile = await _providers.GetMineAsync(User.GetUserId(), cancellationToken);
        return Ok(ApiResponse<ProviderProfileDto>.Ok(profile));
    }

    [HttpPut("me/application")]
    public async Task<ActionResult<ApiResponse<ProviderProfileDto>>> UpdateApplication(
        [FromBody] UpdateProviderApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _providers.UpdateApplicationAsync(User.GetUserId(), request, cancellationToken);
        return Ok(ApiResponse<ProviderProfileDto>.Ok(profile, "Application saved."));
    }

    [HttpPut("me/availability")]
    public async Task<ActionResult<ApiResponse<ProviderProfileDto>>> SetAvailability(
        [FromBody] SetAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _providers.SetAvailabilityAsync(User.GetUserId(), request, cancellationToken);
        return Ok(ApiResponse<ProviderProfileDto>.Ok(profile, "Availability updated."));
    }

    [HttpPost("me/documents")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ProviderDocumentDto>>> UploadDocument(
        [FromForm] Guid documentRequirementId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<ProviderDocumentDto>.Fail("A file is required."));
        }

        await using var stream = file.OpenReadStream();
        var document = await _providers.UploadDocumentAsync(
            User.GetUserId(),
            documentRequirementId,
            file.FileName,
            file.ContentType ?? "application/octet-stream",
            file.Length,
            stream,
            cancellationToken);

        return Ok(ApiResponse<ProviderDocumentDto>.Ok(document, "Document uploaded."));
    }

    [HttpDelete("me/documents/{documentId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteDocument(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await _providers.DeleteDocumentAsync(User.GetUserId(), documentId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Document removed."));
    }

    [HttpGet]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageProviders)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProviderListItemDto>>>> List(
        [FromQuery] ProviderApprovalStatus? approvalStatus,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var providers = await _providers.ListAsync(new ProviderListQuery(approvalStatus, search), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProviderListItemDto>>.Ok(providers));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageProviders)]
    public async Task<ActionResult<ApiResponse<ProviderProfileDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var profile = await _providers.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ProviderProfileDto>.Ok(profile));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageProviders)]
    public async Task<ActionResult<ApiResponse<ProviderProfileDto>>> Approve(
        Guid id,
        [FromBody] ReviewProviderRequest? request,
        CancellationToken cancellationToken)
    {
        var profile = await _providers.ApproveAsync(
            id,
            User.GetUserId(),
            request?.Reason,
            cancellationToken);
        return Ok(ApiResponse<ProviderProfileDto>.Ok(profile, "Provider approved."));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageProviders)]
    public async Task<ActionResult<ApiResponse<ProviderProfileDto>>> Reject(
        Guid id,
        [FromBody] RejectProviderRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _providers.RejectAsync(id, User.GetUserId(), request.Reason, cancellationToken);
        return Ok(ApiResponse<ProviderProfileDto>.Ok(profile, "Provider rejected."));
    }

    [HttpPost("{id:guid}/suspend")]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageProviders)]
    public async Task<ActionResult<ApiResponse<ProviderProfileDto>>> Suspend(
        Guid id,
        [FromBody] SuspendProviderRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _providers.SuspendAsync(id, User.GetUserId(), request.Reason, cancellationToken);
        return Ok(ApiResponse<ProviderProfileDto>.Ok(profile, "Provider suspended."));
    }

    [HttpPost("{id:guid}/reinstate")]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageProviders)]
    public async Task<ActionResult<ApiResponse<ProviderProfileDto>>> Reinstate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var profile = await _providers.ReinstateAsync(id, User.GetUserId(), cancellationToken);
        return Ok(ApiResponse<ProviderProfileDto>.Ok(profile, "Provider reinstated."));
    }

    [HttpPut("{id:guid}/active")]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageProviders)]
    public async Task<ActionResult<ApiResponse<ProviderProfileDto>>> SetActive(
        Guid id,
        [FromBody] SetProviderActiveRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _providers.SetActiveAsync(id, request.IsActive, cancellationToken);
        return Ok(ApiResponse<ProviderProfileDto>.Ok(profile, request.IsActive ? "Provider activated." : "Provider deactivated."));
    }
}
