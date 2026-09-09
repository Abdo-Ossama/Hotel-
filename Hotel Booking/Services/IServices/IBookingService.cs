namespace Hotel_Booking.Services;


public interface IBookingService
{
    Task<BookingResponse> CreateBookingAsync(
        CreateBookingRequest request,
        string userId,
        CancellationToken cancellationToken = default);

    Task CancelBookingAsync(
        Guid bookingId,
        string userId,
        string? cancellationReason,
        CancellationToken cancellationToken = default);

    Task CheckInAsync(
        Guid bookingId,
        string staffUserId,
        CancellationToken cancellationToken = default);

    Task CheckOutAsync(
        Guid bookingId,
        string staffUserId,
        CancellationToken cancellationToken = default);

    Task<BookingDetailsResponse> GetBookingByNumberAsync(
        string bookingNumber,
        string userId,
        bool isStaff,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<MyBookingResponse>> GetMyBookingsAsync(
        string userId,
        int page = 1,
        CancellationToken cancellationToken = default);
}