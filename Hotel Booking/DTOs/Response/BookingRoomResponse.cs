namespace Hotel_Booking.DTOs.Response
{
    public class BookingRoomResponse
    {
        public int RoomId { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public decimal PricePerNight { get; set; }
    }
}
