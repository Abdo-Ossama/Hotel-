using Hotel_Booking.DataAccess;
using Hotel_Booking.DTOs.Response;
using Microsoft.EntityFrameworkCore;
using Hotel_Booking.Repositories.IRepositories;

namespace Hotel_Booking.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserRoleResult>> GetUserRolesAsync(
            List<string> userIds,
            CancellationToken cancellationToken = default)
        {
            if (userIds.Count == 0)
            {
                return [];
            }

            return await _context.UserRoles
                .Where(userRole =>
                    userIds.Contains(userRole.UserId))
                .Join(
                    _context.Roles,
                    userRole => userRole.RoleId,
                    role => role.Id,
                    (userRole, role) => new UserRoleResult
                    {
                        UserId = userRole.UserId,
                        RoleName = role.Name!
                    })
                .ToListAsync(cancellationToken);
        }
    }

}

