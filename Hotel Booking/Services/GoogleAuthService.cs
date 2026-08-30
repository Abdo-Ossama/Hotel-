using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

namespace Hotel_Booking.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly ILogger<GoogleAuthService> _logger;

        public GoogleAuthService(
            ILogger<GoogleAuthService> logger)
        {
            _logger = logger;
        }


        public async Task<GoogleUserInfo> GoogleService(
            HttpContext httpContext)
        {
            // 1. Get Google authentication result
            var result = await httpContext.AuthenticateAsync(
                GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Principal is null)
            {
                _logger.LogWarning(
                    "Google authentication failed.");

                throw new UnauthorizedAccessException(
                    "Google authentication failed.");
            }


            // 2. Get Google Claims

            var principal = result.Principal;

            var email = principal
                .FindFirst(ClaimTypes.Email)?
                .Value;

            var firstName = principal
                .FindFirst(ClaimTypes.GivenName)?
                .Value;

            var lastName = principal
                .FindFirst(ClaimTypes.Surname)?
                .Value;

            var googleId = principal
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;


            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning(
                    "Google login failed because email was not provided.");

                throw new ValidationAppException(
                    "Invalid Google authentication data.",
                    new Dictionary<string, string[]>
                    {
                        ["Email"] =
                        [
                            "Google account email was not found."
                        ]
                    });
            }


            if (string.IsNullOrWhiteSpace(googleId))
            {
                _logger.LogWarning(
                    "Google login failed because Google ID was not provided.");

                throw new ValidationAppException(
                    "Invalid Google authentication data.",
                    new Dictionary<string, string[]>
                    {
                        ["GoogleId"] =
                        [
                            "Google account ID was not found."
                        ]
                    });
            }


            return new GoogleUserInfo
            {
                Email = email,

                GoogleId = googleId,
              
                FirstName =
                    string.IsNullOrWhiteSpace(firstName)
                        ? "User"
                        : firstName,

                LastName =
                    string.IsNullOrWhiteSpace(lastName)
                        ? string.Empty
                        : lastName

                       
            };
        }
    }
}

