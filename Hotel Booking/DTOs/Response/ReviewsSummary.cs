namespace Hotel_Booking.DTOs.Response
{
    public class ReviewsSummary
    {
        public int TotalReviews { get; set; }
        public int ActiveReviews { get; set; }
        public int InactiveReviews { get; set; }
        public decimal AverageRating { get; set; }
    }
}
