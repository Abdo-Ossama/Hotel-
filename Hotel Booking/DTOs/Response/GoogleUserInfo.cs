namespace Hotel_Booking.DTOs.Response
{
    public class GoogleUserInfo
    {
        public string Email { get; set; } = string.Empty;

        public string GoogleId { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string userName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = null!;
    }
}