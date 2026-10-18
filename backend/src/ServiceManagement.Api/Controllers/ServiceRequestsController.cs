using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagement.Api.Extensions;
using ServiceManagement.Application.Common.Models;
using ServiceManagement.Application.Requests.Dtos;
using ServiceManagement.Application.Requests.Interfaces;
using ServiceManagement.Domain.Constants;

namespace ServiceManagement.Api.Controllers;

[ApiController]
[Route("api/service-requests")]
[Authorize]
public class ServiceRequestsController : ControllerBase
{
    private readonly IServiceRequestService _requests;

    public ServiceRequestsController(IServiceRequestService requests)
    {
        _requests = requests;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ServiceRequestDto>>> Place(
        [FromBody] PlaceServiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole(Roles.CommonUser))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse<ServiceRequestDto>.Fail("Only customers can place service requests."));
        }

        var created = await _requests.PlaceAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            ApiResponse<ServiceRequestDto>.Ok(created, "Request placed. Eligible providers can accept it."));
    }

    [HttpGet("mine")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ServiceRequestDto>>>> Mine(
        CancellationToken cancellationToken)
    {
        var items = await _requests.ListMineAsync(User.GetUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ServiceRequestDto>>.Ok(items));
    }

    [HttpGet("inbox")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ServiceRequestDto>>>> Inbox(
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole(Roles.ServiceProvider))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse<IReadOnlyList<ServiceRequestDto>>.Fail("Only providers have an incoming-request inbox."));
        }

        var items = await _requests.ListInboxAsync(User.GetUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ServiceRequestDto>>.Ok(items));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ServiceRequestDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var item = await _requests.GetAsync(
            User.GetUserId(),
            User.IsInRole(Roles.ServiceProvider),
            User.IsInRole(Roles.CommonUser),
            id,
            cancellationToken);
        return Ok(ApiResponse<ServiceRequestDto>.Ok(item));
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<ApiResponse<ServiceRequestDto>>> Accept(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole(Roles.ServiceProvider))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse<ServiceRequestDto>.Fail("Only providers can accept requests."));
        }

        var accepted = await _requests.AcceptAsync(User.GetUserId(), id, cancellationToken);
        return Ok(ApiResponse<ServiceRequestDto>.Ok(accepted, "You accepted this request."));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<ServiceRequestDto>>> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var cancelled = await _requests.CancelAsync(User.GetUserId(), id, cancellationToken);
        return Ok(ApiResponse<ServiceRequestDto>.Ok(cancelled, "Request cancelled."));
    }
}
