namespace Hotel_Booking.DTOs.Request
{
    public class PaymobTransactionObj
    {
        public long Id { get; set; }
        public bool Success { get; set; }
        public bool Pending { get; set; }
        public int AmountCents { get; set; }
        public bool ErrorOccured { get; set; }
        public bool IsRefunded { get; set; }
        public bool IsVoided { get; set; }
        public bool Is3dSecure { get; set; }
        public bool IsAuth { get; set; }
        public bool IsCapture { get; set; }
        public bool HasParentTransaction { get; set; }
        public bool IsStandalonePayment { get; set; }
        public string CreatedAt { get; set; } = "";
        public string Currency { get; set; } = "";
        public long IntegrationId { get; set; }
        public string Owner { get; set; } = "";
        public PaymobOrder Order { get; set; } = default!;
        public PaymobSourceData SourceData { get; set; } = default!;
    }
}
