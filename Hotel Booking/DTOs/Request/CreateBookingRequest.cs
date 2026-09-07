namespace Hotel_Booking.DTOs.Request
{
    public class CreateBookingRequest
    {
        public List<int> RoomIds { get; set; } = new();
        public DateTime CheckIn { get; set; } = DateTime.Now;
        public DateTime CheckOut { get; set; } = DateTime.Now;
        public int GuestCount { get; set; }
        public List<GuestRequest> Guests { get; set; } = new();
    }
}
