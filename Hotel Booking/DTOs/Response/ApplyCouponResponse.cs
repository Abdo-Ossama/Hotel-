namespace Hotel_Booking.DTOs.Response;

public class ApplyCouponResponse
{
    public Guid BookingId { get; set; }

    public string CouponCode { get; set; } = string.Empty;

    public decimal DiscountPercentage { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalBeforeDiscount { get; set; }

    public decimal TotalAfterDiscount { get; set; }
}