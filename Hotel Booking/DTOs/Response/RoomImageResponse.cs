namespace Hotel_Booking.DTOs.Response
{
    public class RoomImageResponse
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }
}
