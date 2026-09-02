namespace Hotel_Booking.DTOs.Response
{
    public class RoomTypeResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal BasePricePerNight { get; set; }

        public int MaxAdults { get; set; }

        public int MaxChildren { get; set; }
    }
}
