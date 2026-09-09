
using Hotel_Booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hotel_Booking.Controllers;

[Area(SD.ADMIN_AREA)]
[Route("api/[area]/bookings")]
[ApiController]
[Authorize(Roles = $"{SD.ADMIN_ROLE},{SD.RECEPTIONIST_ROLE}")]
public class AdminBookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public AdminBookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]

    public async Task<ActionResult<BookingResponse>> CreateBooking(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _bookingService.CreateBookingAsync(
            request,
            adminUserId!,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetBookingByNumber),
            new { bookingNumber = result.BookingNumber },
            result);
    }


    [HttpGet("{bookingNumber}")]
    public async Task<ActionResult<BookingDetailsResponse>> GetBookingByNumber(
        string bookingNumber,
        CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _bookingService.GetBookingByNumberAsync(
            bookingNumber,
            adminUserId!,
            true,
            cancellationToken);

        return Ok(result);
    }




    [HttpPost("{bookingId}/check-in")]
    public async Task<IActionResult> CheckIn(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _bookingService.CheckInAsync(
            bookingId,
            adminUserId!,
            cancellationToken);

        return NoContent();
    }


    [HttpPost("{bookingId}/check-out")]
    public async Task<IActionResult> CheckOut(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _bookingService.CheckOutAsync(
            bookingId,
            adminUserId!,
            cancellationToken);

        return NoContent();
    }
}

