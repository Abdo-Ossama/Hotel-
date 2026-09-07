namespace Hotel_Booking.DTOs.Response
{
    public class BookingResponse
    {
        public Guid Id { get; set; }

        public string BookingNumber { get; set; } = string.Empty;

        public List<int> RoomIds { get; set; } = new();

        public DateTime CheckIn { get; set; }

        public DateTime CheckOut { get; set; }

        public int TotalNights { get; set; }

        public int GuestCount { get; set; }

        public decimal TotalPrice { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime PaymentDueAtUtc { get; set; } 
    }
}
