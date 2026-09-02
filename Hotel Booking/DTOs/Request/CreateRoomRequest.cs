using Hotel_Booking.Enums;

namespace Hotel_Booking.DTOs.Request
{
    public class CreateRoomRequest
    {
        public string RoomNumber { get; set; } = string.Empty;

        public int Floor { get; set; }

        public int RoomTypeId { get; set; }

        public List<int> AmenityIds { get; set; } = [];

     
    }
}
