using Hotel_Booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hotel_Booking.Controllers;

[Area(SD.GUEST_AREA)]
[Route("api/[area]/bookings")]
[ApiController]
[Authorize(Roles = SD.GUEST_ROLE)]
public class GuestBookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public GuestBookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [Authorize(Roles = SD.GUEST_ROLE)]
    [HttpGet("my-bookings")]
    public async Task<ActionResult<PagedResponse<MyBookingResponse>>> GetMyBookings(
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _bookingService.GetMyBookingsAsync(
            userId!,
            page,
            cancellationToken);

        return Ok(result);
    }


    [Authorize(Roles = SD.GUEST_ROLE)]
    [HttpPost("{bookingId}/cancel")]
    public async Task<IActionResult> CancelBooking(
     Guid bookingId,
     [FromBody] CancelBookingRequest? request,
     CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _bookingService.CancelBookingAsync(
            bookingId,
            adminUserId!,
            request?.CancellationReason,
            cancellationToken);

        return NoContent();
    }
}

