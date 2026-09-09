namespace Hotel_Booking.DTOs.Request
{
    public class UpdateReviewRequest
    {
        public int ? Rating { get; set; }
        public string ? Comment { get; set; } = string.Empty;
    }

}
