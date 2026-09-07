using System.Text.Json.Serialization;

public class PaymobBookingRequest
{
    [JsonPropertyName("auth_token")]
    public string AuthToken { get; set; } = null!;

    [JsonPropertyName("delivery_needed")]
    public bool DeliveryNeeded { get; set; }

    [JsonPropertyName("amount_cents")]
    public int AmountCents { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "EGP";

    [JsonPropertyName("merchant_order_id")] 
    public string MerchantBookingId { get; set; } = null!;

    [JsonPropertyName("items")]
    public List<PaymobBookingItem> Items { get; set; } = [];
}