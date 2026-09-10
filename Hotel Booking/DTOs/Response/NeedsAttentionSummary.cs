namespace Hotel_Booking.DTOs.Response
{
    public class NeedsAttentionSummary
    {
        public int PendingBookings { get; set; }
        public int PendingPayments { get; set; }
        public int MaintenanceRooms { get; set; }
    }
}
