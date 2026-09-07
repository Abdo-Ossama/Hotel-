using System.Text.Json.Serialization;

namespace Hotel_Booking.DTOs.Request;

public class PaymobBookingItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = null!;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("amount_cents")]
    public int AmountCents { get; set; }
}