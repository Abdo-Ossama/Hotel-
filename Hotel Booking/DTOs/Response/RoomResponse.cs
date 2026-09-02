
using Hotel_Booking.Enums;

namespace Hotel_Booking.DTOs.Response
{
    public class RoomResponse
    {
        public int Id { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public int Floor { get; set; }

        public RoomStatus Status { get; set; }

        public RoomTypeResponse RoomType { get; set; } = null!;

        public List<AmenityResponse> Amenities { get; set; } = [];

        public List<RoomImageResponse> Images { get; set; } = [];
    }
}
