namespace Hotel_Booking
{
    public static class CancellationPolicy
    {
        public const int FullRefundHours = 48;
        public const int PartialRefundHours = 24;

        public const decimal PartialRefundPercentage = 0.50m;
    }
}
