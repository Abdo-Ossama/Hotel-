namespace Hotel_Booking.DTOs.Request
{
    public class ApplyCouponRequest
    {
        public Guid BookingId { get; set; }

        public string Code { get; set; } = string.Empty;
    }
}
