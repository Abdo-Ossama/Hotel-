namespace Hotel_Booking.DTOs.Response
{
    public class RevenueSummary
    {
        public decimal Today { get; set; }
        public decimal ThisWeek { get; set; }
        public decimal ThisMonth { get; set; }

        public decimal TodayRefunds { get; set; }
        public decimal ThisWeekRefunds { get; set; }
        public decimal ThisMonthRefunds { get; set; }

        public decimal TodayNetRevenue { get; set; }
        public decimal ThisWeekNetRevenue { get; set; }
        public decimal ThisMonthNetRevenue { get; set; }

        public decimal AverageBookingValue { get; set; }

        public decimal? TodayVsYesterdayPercent { get; set; }
    }
}
