

namespace Hotel_Booking.Services.IServices
{
    public interface IGoogleAuthService
    {
       Task<GoogleUserInfo> GoogleService(HttpContext httpContext);
    }
}
