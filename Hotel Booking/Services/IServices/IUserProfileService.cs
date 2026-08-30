using Hotel_Booking.DTOs.Request;
using Hotel_Booking.DTOs.Response;

namespace Hotel_Booking.Services.IServices
{
    public interface IUserProfileService
    {
        Task<UserProfileResponse> GetUserProfile(string userId);

        Task<UserProfileResponse> UpdateUserProfile(
            UpdateUserProfileRequest updateUserProfileRequest,
            string userId);
       

    }
}
