using Hotel_Booking.DTOs.Response;
using Hotel_Booking.Models;

namespace Hotel_Booking.Services.IServices;

public interface ICouponService
{
    Task<ApplyCouponResponse> ApplyCouponAsync(
        string userId,
        ApplyCouponRequest request,
        CancellationToken cancellationToken = default);

    Task RemoveCouponAsync(
        Guid bookingId,
        string userId,
        CancellationToken cancellationToken = default);

    Task ConsumeCouponAsync(
        Booking booking,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Coupon> CreateAsync(
        CreateCouponRequest request,
        CancellationToken cancellationToken = default);
}