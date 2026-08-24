namespace Hotel_Booking.Services.IServices
{
    public interface IJWTHandler
    {
        Task<string?> GenerateTokenAsync(string userId, string email);
    }
}
