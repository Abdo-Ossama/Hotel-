using Hotel_Booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Booking.Controllers;

[Route("api/[area]/[Controller]")]
[Area(SD.ADMIN_AREA)]
[ApiController]
[Authorize(Roles = SD.ADMIN_ROLE)]
public class AdminReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public AdminReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpDelete("{reviewId}")]
    public async Task<IActionResult> DeactivateReview(
        int reviewId,
        CancellationToken cancellationToken)
    {
        await _reviewService.DeactivateReviewAsync(
            reviewId,
            cancellationToken);

        return NoContent();
    }
}