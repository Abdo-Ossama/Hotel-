using Hotel_Booking.Enums;

namespace Hotel_Booking.DTOs.Response
{
    public class BookingSummaryResponse
    {
        public Guid BookingId { get; set; }
        public string BookingNumber { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int TotalNights { get; set; }
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public BookingStatus Status { get; set; }
    }
}
