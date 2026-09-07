using System.Text.Json.Serialization;

public class PaymobBillingData
{
    [JsonPropertyName("apartment")]
    public string Apartment { get; set; } = "NA";

    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    [JsonPropertyName("floor")]
    public string Floor { get; set; } = "NA";

    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = null!;

    [JsonPropertyName("street")]
    public string Street { get; set; } = "NA";

    [JsonPropertyName("building")]
    public string Building { get; set; } = "NA";

    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = null!;

    [JsonPropertyName("shipping_method")]
    public string ShippingMethod { get; set; } = "PKG";

    [JsonPropertyName("postal_code")]
    public string PostalCode { get; set; } = "NA";

    [JsonPropertyName("city")]
    public string City { get; set; } = "Cairo";

    [JsonPropertyName("country")]
    public string Country { get; set; } = "EG";

    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = null!;

    [JsonPropertyName("state")]
    public string State { get; set; } = "Cairo";
}