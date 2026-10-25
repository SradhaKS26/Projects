using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagement.Api.Extensions;
using ServiceManagement.Application.Common.Models;
using ServiceManagement.Application.Reviews.Dtos;
using ServiceManagement.Application.Reviews.Interfaces;
using ServiceManagement.Domain.Constants;

namespace ServiceManagement.Api.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviews;

    public ReviewsController(IReviewService reviews)
    {
        _reviews = reviews;
    }

    [HttpGet("received")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReviewDto>>>> Received(
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole(Roles.ServiceProvider))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse<IReadOnlyList<ReviewDto>>.Fail("Only providers can view received reviews."));
        }

        var items = await _reviews.ListReceivedAsync(User.GetUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ReviewDto>>.Ok(items));
    }
}
