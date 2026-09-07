namespace Hotel_Booking.DTOs.Request;

public class PaymobCallbackPayload
{
    public string? Type { get; set; }

    public PaymobTransactionObj Obj { get; set; } = default!;
}