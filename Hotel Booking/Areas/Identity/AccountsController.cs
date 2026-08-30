
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hotel_Booking.Areas.Identity
{
    [Area(SD.IDENTITY_AREA)]
    [Route("api/[area]/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJWTHandler _jwtHandler;
        private readonly ILogger<AccountsController> _logger;
        private readonly IAccountService _accountService;
        private readonly IGoogleAuthService _googleAuthService;

        public AccountsController(
            UserManager<ApplicationUser> userManager,
            IJWTHandler jwtHandler,
            ILogger<AccountsController> logger,
             IAccountService accountService,
              IGoogleAuthService googleAuthService,
              SignInManager<ApplicationUser> signInManger)

        {
            _userManager = userManager;
            _jwtHandler = jwtHandler;
            _logger = logger;
            _accountService = accountService;
            _googleAuthService = googleAuthService;
       
        }


        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback))
            };

            return Challenge(
                properties,
                GoogleDefaults.AuthenticationScheme);
        }


        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            var googleUser =
                await _googleAuthService.GoogleService(HttpContext);

            var token =
                await _accountService.GoogleLoginAsync(googleUser);

            return Ok(new APIResponse
            {
                Message = [$"Welcome, {googleUser.FirstName}"],
                Data = token
            });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(
         DTOs.Request.LoginRequest loginRequest,
         CancellationToken cancellationToken)
        {
            var token = await _accountService.LoginAsync(
                loginRequest,
                cancellationToken);

            return Ok(new APIResponse
            {
                StatusCode = StatusCodes.Status200OK,
                Message = [$"Welcome, {loginRequest.Email}"],
                Data = token
            });
        }

    }
}