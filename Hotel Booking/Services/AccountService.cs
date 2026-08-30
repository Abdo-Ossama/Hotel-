using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;

namespace Hotel_Booking.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJWTHandler _jwtHandler;
        private readonly ILogger<AccountService> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            IJWTHandler jwtHandler,
            ILogger<AccountService> logger,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _jwtHandler = jwtHandler;
            _logger = logger;
            _signInManager = signInManager;
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
                    UserName = googleUserInfo.userName,
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
                    var errors = createResult.Errors
                        .Select(e => e.Description)
                        .ToArray();

                    throw new ValidationAppException(
                        "Failed to create user.",
                        new Dictionary<string, string[]>
                        {
                            ["User"] = errors
                        });
                }

                _logger.LogInformation(
                    "User {Email} created successfully.",
                    googleUserInfo.Email);
            }

            // Make sure Google user has Guest role
            if (!await _userManager.IsInRoleAsync(
                    user,
                    SD.GUEST_ROLE))
            {
                var roleResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        SD.GUEST_ROLE);

                if (!roleResult.Succeeded)
                {
                    var errors = roleResult.Errors
                        .Select(e => e.Description)
                        .ToArray();

                    throw new ValidationAppException(
                        "Failed to assign Guest role.",
                        new Dictionary<string, string[]>
                        {
                            ["Role"] = errors
                        });
                }

                _logger.LogInformation(
                    "Guest role assigned to user {Email}.",
                    googleUserInfo.Email);
            }

            if (user.IsBlocked)
            {
                throw new ForbiddenException(
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
                    var errors = loginResult.Errors
                        .Select(e => e.Description)
                        .ToArray();

                    throw new ValidationAppException(
                        "Failed to link Google account.",
                        new Dictionary<string, string[]>
                        {
                            ["GoogleLogin"] = errors
                        });
                }

                _logger.LogInformation(
                    "Google account linked successfully to {Email}.",
                    googleUserInfo.Email);
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
                throw new InvalidOperationException(
                    "Failed to generate authentication token.");
            }

            _logger.LogInformation(
                "Google login completed successfully for user {Email}.",
                googleUserInfo.Email);

            return token;
        }


        public async Task<string> LoginAsync(
    LoginRequest loginRequest,
    CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(
                loginRequest.Email);

            if (user is null)
            {
                throw new NotFoundException(
                    "Invalid username or password.");
            }

            if (user.IsBlocked)
            {
                throw new ForbiddenException(
                    "Your account has been blocked.");
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                loginRequest.Password,
                loginRequest.RememberMe,
                lockoutOnFailure: true);

            if (result.IsNotAllowed)
            {
                throw new UnauthorizedAccessException(
                    "Please confirm your email first.");
            }

            if (result.IsLockedOut)
            {
                throw new UnauthorizedAccessException(
                    "Your account has been locked due to multiple failed login attempts.");
            }

            if (!result.Succeeded)
            {
                throw new NotFoundException(
                    "Invalid username or password.");
            }

            _logger.LogInformation(
                "Generating JWT for user {Email}.",
                user.Email);

            var token = await _jwtHandler.GenerateTokenAsync(
                user.Id,
                user.Email!);

            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException(
                    "Failed to generate authentication token.");
            }

            _logger.LogInformation(
                "Login completed successfully for user {Email}.",
                user.Email);

            return token;
        }

    }
}