using System.Text.Json.Serialization;

namespace Hotel_Booking.DTOs.Response;

public class PaymobPaymentKeyResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = null!;
}