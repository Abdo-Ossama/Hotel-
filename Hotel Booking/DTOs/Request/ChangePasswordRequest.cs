namespace Hotel_Booking.DTOs.Request
{
    public class ChangePasswordRequest
    {
      
        public string currentPassword { get; set; } = string.Empty;
        public string newPassword { get; set; } = string.Empty;
    }
}
