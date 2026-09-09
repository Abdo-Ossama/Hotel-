using Hotel_Booking.DTOs.Response;
using Hotel_Booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Booking.Controllers;

[Route("api/[Controller]")]  
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponse<ReviewResponse>>> GetReviews(
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _reviewService.GetReviewsAsync(
            page,
            cancellationToken);

        return Ok(result);
    }
}