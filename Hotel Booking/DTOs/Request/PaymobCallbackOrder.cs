using System.Text.Json.Serialization;

namespace Hotel_Booking.DTOs.Request;

public class PaymobCallbackOrder
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

 
    [JsonPropertyName("merchant_order_id")]
    public string? MerchantBookingId { get; set; }
}
