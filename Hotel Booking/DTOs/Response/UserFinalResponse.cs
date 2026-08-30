namespace Hotel_Booking.DTOs.Response
{
    public class UserFinalResponse
    {
        public int Page { get; set; }

    

        public int TotalUsers { get; set; }

        public int TotalPages { get; set; }

        public List<UserResponse> Data { get; set; } = new();
    }
}
