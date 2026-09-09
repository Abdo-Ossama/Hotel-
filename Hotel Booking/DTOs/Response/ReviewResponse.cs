namespace Hotel_Booking.DTOs.Response;

public class ReviewResponse
{
    public int Id { get; set; }
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}