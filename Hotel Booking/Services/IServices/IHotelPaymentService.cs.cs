

namespace Hotel_Booking.Services;

public interface IHotelPaymentService
{
    Task<string> CreateCheckoutAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task HandlePaymentCallbackAsync(
        PaymobTransactionObj transaction,
        CancellationToken cancellationToken = default);
}
