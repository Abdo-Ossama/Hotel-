namespace Hotel_Booking.DTOs.Response
{
    public class BookingDetailsResponse
    {
        public Guid Id { get; set; }

        public string BookingNumber { get; set; } = string.Empty;

        public DateTime CheckIn { get; set; }

        public DateTime CheckOut { get; set; }

        public int TotalNights { get; set; }

        public int GuestCount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TotalPrice { get; set; }

        public string Status { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public DateTime PaymentDueAtUtc { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<BookingRoomResponse> Rooms { get; set; } = new();

        public List<BookingGuestResponse> Guests { get; set; } = new();
    }
}
