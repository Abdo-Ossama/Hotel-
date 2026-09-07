namespace Hotel_Booking.Utilites;

public class PaymobSettings
{
    public string BaseUrl { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public long IntegrationId { get; set; }
    public string IframeId { get; set; } = null!;
    public string HmacSecret { get; set; } = null!;
}