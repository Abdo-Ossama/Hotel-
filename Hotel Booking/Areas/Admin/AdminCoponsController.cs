using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hotel_Booking.Controllers;

[Area(SD.ADMIN_AREA)]
[ApiController]
[Route("api/[area]/[controller]")]
[Authorize(Roles = SD.ADMIN_ROLE + "," + SD.RECEPTIONIST_ROLE)]
public class CouponsController : ControllerBase
{
    private readonly ICouponService _couponService;

    public CouponsController(ICouponService couponService)
    {
        _couponService = couponService;
    }

    [HttpPost]
    [Authorize(Roles = SD.ADMIN_ROLE + "," + SD.RECEPTIONIST_ROLE)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCouponRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _couponService.CreateAsync(
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new APIResponse
            {
                StatusCode = StatusCodes.Status201Created,
                Message = ["Coupon created successfully."],
                Data = result
            });
    }


    [HttpDelete("{bookingId}")]
    public async Task<IActionResult> RemoveCoupon(
           Guid bookingId,
           CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException(
                "User not authenticated.");

        await _couponService.RemoveCouponAsync(
            bookingId,
            userId,
            cancellationToken);

        return Ok(new APIResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Message =
            [
                "Coupon removed successfully."
            ]
        });
    }


    [Authorize(Roles = SD.ADMIN_ROLE + "," + SD.RECEPTIONIST_ROLE)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        await _couponService.DeleteAsync(
            id,
            cancellationToken);

        return Ok(new APIResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Message = ["Coupon deactivated successfully."]
        });



       
    } }