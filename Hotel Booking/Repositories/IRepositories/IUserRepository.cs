using Hotel_Booking.DTOs.Response;

namespace Hotel_Booking.Repositories.IRepositories
{
   public interface IUserRepository
        {
        Task<List<UserRoleResult>> GetUserRolesAsync(
         List<string> userIds,
         CancellationToken cancellationToken = default);
        }
    }

