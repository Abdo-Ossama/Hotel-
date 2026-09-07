using System.Text.Json.Serialization;

namespace Hotel_Booking.DTOs.Request;

public class PaymobAuthRequest
{
    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = null!;
}