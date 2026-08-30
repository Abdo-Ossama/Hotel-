using Hotel_Booking.Repositories.IRepositories;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hotel_Booking.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserRepository _userRepository;



        private static readonly string[] AssignableRoles =
        {
            SD.ADMIN_ROLE,
            SD.RECEPTIONIST_ROLE
        };

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserRepository userRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userRepository = userRepository;
        }


        public async Task<UserFinalResponse> GetPaginatedUsersAsync(
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            if (page <= 0)
            {
                page = 1;
            }

            const int pageSize = 5;

            var usersQuery = _userManager.Users
                .AsNoTracking()
                .OrderBy(e => e.Id);

            var totalUsers = await usersQuery
                .CountAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling(
                totalUsers / (double)pageSize);

            var users = await usersQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new UserResponse
                {
                    Id = e.Id,
                    UserName = e.UserName!,
                    Email = e.Email!,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    PhoneNumber = e.PhoneNumber,
                    IsBlocked = e.IsBlocked,
                    Roles = new List<string>()
                })
                .ToListAsync(cancellationToken);

            var userIds = users
                .Select(e => e.Id)
                .ToList();

            var userRoles = await _userRepository
                .GetUserRolesAsync(
                    userIds,
                    cancellationToken);

            foreach (var user in users)
            {
                user.Roles = userRoles
                    .Where(x => x.UserId == user.Id)
                    .Select(x => x.RoleName)
                    .ToList();
            }

            return new UserFinalResponse
            {
                Page = page,
                TotalUsers = totalUsers,
                TotalPages = totalPages,
                Data = users
            };
        }


        public async Task<UserResponse> GetUserAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ValidationAppException(
                    "Invalid Request.",
                    new Dictionary<string, string[]>
                    {
                        ["Id"] =
                        [
                            "User ID is required."
                        ]
                    });
            }

            var user =
                await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                throw new NotFoundException(
                    "User is not found.");
            }

            var roles =
                await _userManager.GetRolesAsync(user);

            return new UserResponse
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsBlocked = user.IsBlocked,
                Roles = roles.ToList()
            };
        }


        public async Task CreateStaffAsync(
            CreateStaffRequest createStaffRequest,
            CancellationToken cancellationToken = default)
        {


            if (!AssignableRoles.Contains(createStaffRequest.Role))
            {
                throw new ValidationAppException(
                    "Invalid role.",
                    new Dictionary<string, string[]>
                    {
                        ["Role"] =
                        [
                            $"Role '{createStaffRequest.Role}' cannot be assigned here."
                        ]
                    });
            }



            var roleExists =
                await _roleManager.RoleExistsAsync(
                    createStaffRequest.Role);

            if (!roleExists)
            {
                throw new ValidationAppException(
                    "Invalid role.",
                    new Dictionary<string, string[]>
                    {
                        ["Role"] =
                        [
                            $"Role '{createStaffRequest.Role}' does not exist."
                        ]
                    });
            }


            var existingUser = await _userManager.FindByNameAsync(createStaffRequest.UserName);

            if (existingUser is not null)
            {
                throw new ConflictException(
                    "Username is already registered.");
            }

            var user = new ApplicationUser
            {
                UserName = createStaffRequest.UserName,
                Email = createStaffRequest.Email,
                FirstName = createStaffRequest.firstName,
                LastName = createStaffRequest.lastName
            };

            var createResult = await _userManager.CreateAsync(user, createStaffRequest.Password);


            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .Select(e => e.Description)
                            .ToArray());



                throw new ValidationAppException(
              $"Failed to create user: {string.Join(" | ",
                createResult.Errors.Select(e =>
                $"{e.Code}: {e.Description}"))}",
                errors);
            }


            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    createStaffRequest.Role);

            if (!roleResult.Succeeded)
            {
                // Rollback user creation
                await _userManager.DeleteAsync(user);

                var errors = roleResult.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .Select(e => e.Description)
                            .ToArray());

                throw new BusinessRuleException(
                    string.Join(
                        ", ",
                        errors.SelectMany(
                            e => e.Value)));
            }
            
        }


        public async Task UpdateUserRolesAsync(
            string userId,
            UpdateUserRolesRequest  updateUserRolesRequest,
            string currentUserId,
            CancellationToken cancellationToken = default)
        {


            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ValidationAppException(
                    "Invalid createStaffRequest.",
                    new Dictionary<string, string[]>
                    {
                        ["Id"] =
                        [
                            "User ID is required."
                        ]
                    });
            }

            if (updateUserRolesRequest.Roles is null ||
                !updateUserRolesRequest.Roles.Any())
            {
                throw new ValidationAppException(
                    "Invalid createStaffRequest.",
                    new Dictionary<string, string[]>
                    {
                        ["Roles"] =
                        [
                            "At least one role is required."
                        ]
                    });
            }



            var invalidRoles = updateUserRolesRequest.Roles
                .Except(AssignableRoles)
                .ToList();

            if (invalidRoles.Any())
            {
                throw new ValidationAppException(
                    "Invalid roles.",
                    new Dictionary<string, string[]>
                    {
                        ["Roles"] =
                            invalidRoles
                                .Select(role =>
                                    $"Role '{role}' cannot be assigned here.")
                                .ToArray()
                    });
            }


            foreach (var role in updateUserRolesRequest.Roles)
            {
                var roleExists =
                    await _roleManager.RoleExistsAsync(role);

                if (!roleExists)
                {
                    throw new ValidationAppException(
                        "Invalid role.",
                        new Dictionary<string, string[]>
                        {
                            ["Roles"] =
                            [
                                $"Role '{role}' does not exist."
                            ]
                        });
                }
            }


            var user =
                await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                throw new NotFoundException(
                    "User not found.");
            }


            if (user.Id == currentUserId)
            {
                throw new ForbiddenException(
                    "You cannot modify your own account roles.");
            }


            var isAdmin =
                await _userManager.IsInRoleAsync(
                    user,
                    SD.ADMIN_ROLE);

            if (isAdmin)
            {
                var admins =
                    await _userManager.GetUsersInRoleAsync(
                        SD.ADMIN_ROLE);

                if (admins.Count <= 1)
                {
                    throw new ForbiddenException(
                        "Cannot modify the last remaining Admin account.");
                }
            }


            var currentRoles =
                await _userManager.GetRolesAsync(user);

            var rolesToAdd = updateUserRolesRequest.Roles
                .Except(currentRoles)
                .ToList();

            var rolesToRemove = currentRoles
                .Except(updateUserRolesRequest.Roles)
                .ToList();



            if (rolesToRemove.Any())
            {
                var removeResult =
                    await _userManager.RemoveFromRolesAsync(
                        user,
                        rolesToRemove);

                if (!removeResult.Succeeded)
                {
                    var errors = removeResult.Errors
                        .GroupBy(e => e.Code)
                        .ToDictionary(
                            g => g.Key,
                            g => g
                                .Select(e => e.Description)
                                .ToArray());

                    throw new BusinessRuleException(
                        string.Join(
                            ", ",
                            errors.SelectMany(
                                e => e.Value)));
                }
            }


            if (rolesToAdd.Any())
            {
                var addResult =
                    await _userManager.AddToRolesAsync(
                        user,
                        rolesToAdd);

                if (!addResult.Succeeded)
                {
                    var errors = addResult.Errors
                        .GroupBy(e => e.Code)
                        .ToDictionary(
                            g => g.Key,
                            g => g
                                .Select(e => e.Description)
                                .ToArray());

                    throw new BusinessRuleException(
                        string.Join(
                            ", ",
                            errors.SelectMany(
                                e => e.Value)));
                }
            }
        }


        public async Task ToggleUserStatusAsync(
            string userId,
            string currentUserId,
            CancellationToken cancellationToken = default)
        {


            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ValidationAppException(
                    "Invalid createStaffRequest.",
                    new Dictionary<string, string[]>
                    {
                        ["Id"] =
                        [
                            "User ID is required."
                        ]
                    });
            }


            var user =
                await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                throw new NotFoundException(
                    "User is not found.");
            }


            if (user.Id == currentUserId)
            {
                throw new ForbiddenException(
                    "You cannot modify your own account status.");
            }


            var isAdmin =
                await _userManager.IsInRoleAsync(
                    user,
                    SD.ADMIN_ROLE);

            if (isAdmin)
            {
                var admins =
                    await _userManager.GetUsersInRoleAsync(
                        SD.ADMIN_ROLE);

                if (admins.Count <= 1)
                {
                    throw new ForbiddenException(
                        "Cannot modify the last remaining Admin account.");
                }
            }


            user.IsBlocked = !user.IsBlocked;

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .Select(e => e.Description)
                            .ToArray());

                throw new BusinessRuleException(
                    string.Join(
                        ", ",
                        errors.SelectMany(
                            e => e.Value)));
            }
        }
    }
}