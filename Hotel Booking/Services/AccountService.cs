using Hotel_Booking.Models;
using Hotel_Booking.Models.DTOs.Request;
using Hotel_Booking.Services.IServices;
using Hotel_Booking.Utilites;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;

namespace Hotel_Booking.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJWTHandler _jwtHandler;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            IJWTHandler jwtHandler,
            ILogger<AccountService> logger)
        { 
            _userManager = userManager;
            _jwtHandler = jwtHandler;
            _logger = logger;
        }

        public async Task<string> GoogleLoginAsync(
            GoogleUserInfo googleUserInfo)
        {
      
            var user = await _userManager.FindByEmailAsync(
                googleUserInfo.Email);



            if (user is null)
            {
                _logger.LogInformation(
                    "Creating new user from Google account {Email}.",
                    googleUserInfo.Email);

                user = new ApplicationUser
                {
                    UserName = googleUserInfo.Email,
                    Email = googleUserInfo.Email,

                    FirstName = googleUserInfo.FirstName,
                    LastName = googleUserInfo.LastName,

                    EmailConfirmed = true,

                    CreatedAtUtc = DateTime.UtcNow,

                    IsBlocked = false
                };


                var createResult =
                    await _userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                {
                    _logger.LogError(
                        "Failed to create user {Email}.",
                        googleUserInfo.Email);

                    throw new InvalidOperationException(
                        string.Join(
                            ", ",
                            createResult.Errors.Select(
                                e => e.Description)));
                }



                var roleResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        SD.GUEST_ROLE);

                if (!roleResult.Succeeded)
                {
                    _logger.LogError(
                        "Failed to add Guest role to {Email}.",
                        googleUserInfo.Email);

                    throw new InvalidOperationException(
                        string.Join(
                            ", ",
                            roleResult.Errors.Select(
                                e => e.Description)));
                }

                _logger.LogInformation(
                    "User {Email} created successfully.",
                    googleUserInfo.Email);
            }


            if (user.IsBlocked)
            {
                _logger.LogWarning(
                    "Blocked user {Email} attempted login.",
                    googleUserInfo.Email);

                throw new UnauthorizedAccessException(
                    "Your account has been blocked.");
            }


            var userLogins =
                await _userManager.GetLoginsAsync(user);

            var googleLoginExists =
                userLogins.Any(login =>
                    login.LoginProvider ==
                    GoogleDefaults.AuthenticationScheme
                    &&
                    login.ProviderKey ==
                    googleUserInfo.GoogleId);


            if (!googleLoginExists)
            {
                _logger.LogInformation(
                    "Linking Google account to {Email}.",
                    googleUserInfo.Email);

                var loginInfo = new UserLoginInfo(
                    GoogleDefaults.AuthenticationScheme,
                    googleUserInfo.GoogleId,
                    "Google");


                var loginResult =
                    await _userManager.AddLoginAsync(
                        user,
                        loginInfo);


                if (!loginResult.Succeeded)
                {
                    _logger.LogError(
                        "Failed to link Google account to {Email}.",
                        googleUserInfo.Email);

                    throw new InvalidOperationException(
                        string.Join(
                            ", ",
                            loginResult.Errors.Select(
                                e => e.Description)));
                }
            }



            _logger.LogInformation(
                "Generating JWT for user {Email}.",
                googleUserInfo.Email);

            var token =
                await _jwtHandler.GenerateTokenAsync(
                    user.Id,
                    user.Email!);

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Token generation failed for user {Email}.", googleUserInfo.Email);
                throw new InvalidOperationException("Failed to generate authentication token.");
            }

            return token;


       
        }
    }
}