

namespace Hotel_Booking.Services.IServices
{
    public interface IAccountService
    {
        Task<string> GoogleLoginAsync(GoogleUserInfo googleUserInfo);
        Task<string> LoginAsync(
       LoginRequest loginRequest,
       CancellationToken cancellationToken = default);
    }
}
