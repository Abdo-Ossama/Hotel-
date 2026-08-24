
namespace Hotel_Booking.Models;

public class CouponUsage
{
    public int Id { get; set; }

    public int CouponId { get; set; }

    public Coupon Coupon { get; set; } = null!;

    public string CustomerId { get; set; } = null!; 

    public ApplicationUser Customer { get; set; } = null!;

    public Guid BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    public DateTime UsedAtUtc { get; set; }
        = DateTime.UtcNow;
}