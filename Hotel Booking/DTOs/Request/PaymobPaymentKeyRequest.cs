using System.Text.Json.Serialization;

namespace Hotel_Booking.DTOs.Request;

public class PaymobPaymentKeyRequest
{
    [JsonPropertyName("auth_token")]
    public string AuthToken { get; set; } = null!;

    [JsonPropertyName("amount_cents")]
    public int AmountCents { get; set; }

    [JsonPropertyName("expiration")]
    public int Expiration { get; set; }

    [JsonPropertyName("order_id")]
    public long PaymobBookingId { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "EGP";

    [JsonPropertyName("integration_id")]
    public long IntegrationId { get; set; }

    [JsonPropertyName("billing_data")]
    public PaymobBillingData BillingData { get; set; } = null!;
}