using Hotel_Booking.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace Hotel_Booking.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserProfileService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<UserProfileResponse> GetUserProfile(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ValidationAppException(
                    "Invalid user ID.",
                    new Dictionary<string, string[]>
                    {
                        ["UserId"] =
                        [
                            "User ID is required."
                        ]
                    });
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                throw new NotFoundException(
                    "User is not found.");
            }

            if (user.IsBlocked)
            {
                throw new ForbiddenException(
                    "Your account is blocked.");
            }

            var roles = await _userManager.GetRolesAsync(user);

            return new UserProfileResponse
            {
                userName = user.UserName!,
                Email = user.Email!,
                firstName = user.FirstName,
                lastName = user.LastName,
                Roles = roles.ToList()
            };
        }


        public async Task<UserProfileResponse> UpdateUserProfile(
            UpdateUserProfileRequest updateUserProfileRequest,
            string userId)
        {
            if (updateUserProfileRequest is null)
            {
                throw new ValidationAppException(
                    "Validation failed.",
                    new Dictionary<string, string[]>
                    {
                        ["Request"] =
                        [
                            "Update profile request is required."
                        ]
                    });
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ValidationAppException(
                    "Invalid user ID.",
                    new Dictionary<string, string[]>
                    {
                        ["UserId"] =
                        [
                            "User ID is required."
                        ]
                    });
            }

            var user = await _userManager.FindByIdAsync(
                userId);

            if (user is null)
            {
                throw new NotFoundException(
                    "User is not found.");
            }

            if (user.IsBlocked)
            {
                throw new ForbiddenException(
                    "Your account is blocked.");
            }

            user.FirstName =
                updateUserProfileRequest.firstName
                ?? user.FirstName;

            user.LastName =
                updateUserProfileRequest.lastName
                ?? user.LastName;


            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    result.Errors.Select(e => e.Description));

                throw new ValidationAppException(
                    "Failed to update profile.",
                    new Dictionary<string, string[]>
                    {
                        ["Profile"] =
                        [
                            errors
                        ]
                    });
            }

            var roles = await _userManager.GetRolesAsync(user);

            return new UserProfileResponse
            {
                userName = user.UserName!,
                Email = user.Email!,
                firstName = user.FirstName,
                lastName = user.LastName,
                Roles = roles.ToList()
            };
        }


    }
}
