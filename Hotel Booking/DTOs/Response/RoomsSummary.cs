namespace Hotel_Booking.DTOs.Response
{
    public class RoomsSummary
    {
        public int Total { get; set; }
        public int Available { get; set; }
        public int Reserved { get; set; }
        public int Occupied { get; set; }
        public int Cleaning { get; set; }
        public int Maintenance { get; set; }
        public int OutOfService { get; set; }
        public decimal OccupancyRate { get; set; }
    }
}
