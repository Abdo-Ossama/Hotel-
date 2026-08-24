using Hotel_Booking.Models.DTOs.Request;

namespace Hotel_Booking.Services.IServices
{
    public interface IAccountService
    {
        Task<string> GoogleLoginAsync(GoogleUserInfo googleUserInfo);
    }
}
