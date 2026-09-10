using Hotel_Booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hotel_Booking.Controllers;

[Area(SD.GUEST_AREA)]
[Route("api/[area]/[controller]")]
[ApiController]
[Authorize(Roles = SD.GUEST_ROLE)] 
public class GuestCouponController : ControllerBase
{
    private readonly ICouponService _couponService;

    public GuestCouponController(ICouponService couponService)
    {
        _couponService = couponService;
    }

    [HttpPost("apply")]
    public async Task<IActionResult> ApplyCoupon(
        [FromBody] ApplyCouponRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("User not authenticated.");

        var result = await _couponService.ApplyCouponAsync(
            userId,
            request,
            cancellationToken);

        return Ok(new APIResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Message = ["Coupon applied successfully."],
            Data = result
        });
    }

    [HttpDelete("{bookingId}")]
    public async Task<IActionResult> RemoveCoupon(
         Guid bookingId,
         CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("User not authenticated.");

        await _couponService.RemoveCouponAsync(
            bookingId,
            userId,
            cancellationToken);

        return Ok(new APIResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Message = ["Coupon removed successfully."]
        });
    }
}