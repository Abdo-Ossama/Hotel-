using Hotel_Booking.DTOs.Request;
using Hotel_Booking.DTOs.Response;

namespace Hotel_Booking.Services.IServices
{
    public interface IUserService
    {
        Task<UserFinalResponse> GetPaginatedUsersAsync(
            int page,
            CancellationToken cancellationToken = default);

        Task<UserResponse> GetUserAsync(
            string userId,
            CancellationToken cancellationToken = default);

        Task CreateStaffAsync(
            CreateStaffRequest request,
            CancellationToken cancellationToken = default);

        Task UpdateUserRolesAsync(
            string userId,
            UpdateUserRolesRequest request,
            string currentUserId,
            CancellationToken cancellationToken = default);

        Task ToggleUserStatusAsync(
            string userId,
            string currentUserId,
            CancellationToken cancellationToken = default);
    }
}