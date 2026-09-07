namespace Hotel_Booking.DTOs.Response;

public class MyBookingResponse
{
    public Guid Id { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int TotalNights { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

}