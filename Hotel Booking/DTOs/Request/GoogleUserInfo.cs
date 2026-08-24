namespace Hotel_Booking.Models.DTOs.Request
{
    public class GoogleUserInfo
    {
        public string Email { get; set; } = string.Empty;

        public string GoogleId { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;
    }
}