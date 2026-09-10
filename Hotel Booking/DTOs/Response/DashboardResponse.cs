namespace Hotel_Booking.DTOs.Response
{
    public class DashboardResponse
    {
        public DateTime GeneratedAtUtc { get; set; }
        public string Currency { get; set; } = "EGP";

        public RevenueSummary Revenue { get; set; } = new();
        public BookingsSummary Bookings { get; set; } = new();
        public RoomsSummary Rooms { get; set; } = new();
        public BookingPerformanceSummary BookingPerformance { get; set; } = new();
        public CustomersSummary Customers { get; set; } = new();
        public ReviewsSummary Reviews { get; set; } = new();
        public NeedsAttentionSummary NeedsAttention { get; set; } = new();
    }
}
