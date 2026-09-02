using Hotel_Booking.Enums;

namespace Hotel_Booking.DTOs.Request
{
    public class UpdateRoomStatusRequest
    {
        public RoomStatus? Status { get; set; }
    }
}
