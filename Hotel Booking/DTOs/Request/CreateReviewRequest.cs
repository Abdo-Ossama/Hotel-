namespace Hotel_Booking.DTOs.Request
{
    public class CreateReviewRequest
    {
        public Guid BookingId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }

}
