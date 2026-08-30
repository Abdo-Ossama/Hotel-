
using Hotel_Booking.DTOs.Request;
using Hotel_Booking.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hotel_Booking.Areas.Guest
{
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area(SD.GUEST_AREA)]
    [Authorize]

    public class ProfileController : ControllerBase
    {
      private readonly IUserProfileService _userService;

        public ProfileController(IUserProfileService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
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

            var userProfile = await _userService.GetUserProfile(userId);
            return Ok(userProfile);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile(UpdateUserProfileRequest updateUserProfileRequest)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
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

            var userProfile = await _userService.UpdateUserProfile(updateUserProfileRequest,userId);
            return Ok(new APIResponse
            {
                Message = ["Updated Successfully"]
            });
        }
 
       
    }
        }
    

