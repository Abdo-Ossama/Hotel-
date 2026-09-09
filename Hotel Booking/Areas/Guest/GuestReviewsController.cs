using Hotel_Booking.DTOs.Request;
using Hotel_Booking.DTOs.Response;
using Hotel_Booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hotel_Booking.Controllers;

[Area(SD.GUEST_AREA)]
[Route("api/[area]/Reviews")]
[ApiController]
[Authorize(Roles  = SD.GUEST_ROLE)]
public class GuestReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public GuestReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost]
    public async Task<ActionResult<ReviewResponse>> CreateReview(
        [FromBody] CreateReviewRequest createReviewRequest,
        CancellationToken cancellationToken)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _reviewService.CreateReviewAsync(
            createReviewRequest,
            customerId!,
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("{reviewId}")]
    public async Task<ActionResult<ReviewResponse>> UpdateReview(
        int reviewId,
        [FromBody] UpdateReviewRequest updateReviewRequest,
        CancellationToken cancellationToken)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _reviewService.UpdateReviewAsync(
            reviewId,
            updateReviewRequest,
            customerId!,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{reviewId}")]
    public async Task<IActionResult> DeactivateReview(
        int reviewId,
        CancellationToken cancellationToken)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _reviewService.DeactivateOwnReviewAsync(
            reviewId,
            customerId!,
            cancellationToken);

        return NoContent();
    }
}