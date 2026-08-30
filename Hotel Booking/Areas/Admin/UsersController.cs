using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hotel_Booking.Controllers
{
    [ApiController]
    [Area(SD.ADMIN_ROLE)]
    [Route("api/[Area]/[controller]")]
    [Authorize(Roles = SD.ADMIN_ROLE + "," + SD.RECEPTIONIST_ROLE)]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [Authorize(Roles = SD.ADMIN_ROLE + "," + SD.RECEPTIONIST_ROLE)]

        [HttpGet("")]
        public async Task<ActionResult<UserFinalResponse>> GetUsers(
            [FromQuery] int page = 1,
            CancellationToken cancellationToken = default)
        {
            var result = await _userService.GetPaginatedUsersAsync(page, cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = SD.ADMIN_ROLE + "," + SD.RECEPTIONIST_ROLE)]
        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponse>> GetUserById(
            string id,
            CancellationToken cancellationToken = default)
        {
            var user = await _userService.GetUserAsync(id, cancellationToken);
            return Ok(user);
        }

        [Authorize(Roles = SD.ADMIN_ROLE + "," + SD.RECEPTIONIST_ROLE)]
        [HttpPost("staff")]
        public async Task<IActionResult> CreateStaff(
            [FromBody] CreateStaffRequest createStaffRequest,
            CancellationToken cancellationToken = default)
        {
            await _userService.CreateStaffAsync(createStaffRequest, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new { message = "Staff account created successfully." });
        }

        [Authorize(Roles = SD.ADMIN_ROLE )]

        [HttpPut("{id}/roles")]
        public async Task<IActionResult> UpdateUserRoles(
            string id,
            [FromBody] UpdateUserRolesRequest updateUserRolesRequest,
            CancellationToken cancellationToken = default)
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

            await _userService.UpdateUserRolesAsync(id, updateUserRolesRequest, userId, cancellationToken);
            return Ok(new { message = "User roles updated successfully." });
        }

        [Authorize(Roles = SD.ADMIN_ROLE + "," + SD.RECEPTIONIST_ROLE)]

        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(
            string id,
            CancellationToken cancellationToken = default)
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
            await _userService.ToggleUserStatusAsync(id, userId, cancellationToken);
            return Ok(new { message = "User status updated successfully." });
        }


    }
}