namespace Hotel_Booking.DTOs.Response
{
    public class BookingsSummary
    {
        public int Today { get; set; }
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Confirmed { get; set; }
        public int CheckedIn { get; set; }
        public int CheckedOut { get; set; }
        public int Cancelled { get; set; }

        public int Expired { get; set; }
        public int NoShow { get; set; }
    }
}
