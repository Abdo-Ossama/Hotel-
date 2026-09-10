namespace Hotel_Booking.DTOs.Response
{
    public class BookingPerformanceSummary
    {
        public string MostBookedRoomType { get; set; } = string.Empty;
        public int MostBookedRoomTypeCount { get; set; }
    }
}
