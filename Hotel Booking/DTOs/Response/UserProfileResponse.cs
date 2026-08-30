namespace Hotel_Booking.DTOs.Response
{
    public class UserProfileResponse
    {
     
        public string firstName { get; set; } = string.Empty;

        public string lastName { get; set; } = string.Empty;
        public string userName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = null!;
    }
}
