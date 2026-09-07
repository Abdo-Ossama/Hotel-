using System.Text.Json.Serialization;

namespace Hotel_Booking.DTOs.Response;

public class PaymobBookingResponse
{
    [JsonPropertyName("id")]
    public long PaymobBookingId { get; set; }
}