using Hotel_Booking.Enums;
using Hotel_Booking.Repositories.IRepositories;
using Hotel_Booking.Services.Hotel_Booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;

namespace Hotel_Booking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RoomsController : ControllerBase
    {

        private readonly IRoomService _roomService;

        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        // get all avalible rooms 

        [HttpGet("Avaliable")]
        public async Task<IActionResult> GetAvaliableRooms(
      int page = 1,
      CancellationToken cancellationToken = default)
        {
            var result = await _roomService.GetAvailableRoomsAsync(
                page,
                cancellationToken);

            return Ok(new APIResponse
            {
                StatusCode = StatusCodes.Status200OK,
                Message = ["Data Retrieved Successfully."],
                Data = result
            });
        }

        //get all rooms
        [Authorize(Roles = SD.ADMIN_ROLE + "," + SD.RECEPTIONIST_ROLE)]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            var result = await _roomService.GetAllRoomsAsync(
                page,
                cancellationToken);

            return Ok(new APIResponse
            {
                StatusCode = StatusCodes.Status200OK,
                Message = ["Data Retrieved Successfully."],
                Data = result
            });
        }
        [Authorize(Roles = SD.ADMIN_ROLE + "," + SD.RECEPTIONIST_ROLE)]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _roomService.GetRoomByIdAsync(
                id,
                cancellationToken);

            return Ok(new APIResponse
            {
                StatusCode = StatusCodes.Status200OK,
                Message = ["Room retrieved successfully."],
                Data = result
            });
        }

        [Authorize(Roles = SD.ADMIN_ROLE + "," + SD.RECEPTIONIST_ROLE)]
        [HttpPost]
        public async Task<IActionResult> Create(
    [FromForm] CreateRoomRequest createRoomRequest,
     [FromForm]List<IFormFile > images,
    CancellationToken cancellationToken)
        {
            await _roomService.CreateRoomAsync(
                 createRoomRequest,
                  images,
                cancellationToken);

            return Ok(new APIResponse
            {
                Message = ["Room created successfully."]
            });
        }


        [HttpPut("{id}")]
        [Authorize(Roles = SD.ADMIN_ROLE + "," + SD.RECEPTIONIST_ROLE)]
        public async Task<IActionResult> UpdateRoom(
    int id,
    [FromForm] UpdateRoomRequest  updateRoomRequest,
    [FromForm] List<IFormFile> images,
    CancellationToken cancellationToken)
        {
            await _roomService.UpdateRoomAsync(
                id,
                updateRoomRequest,
                images,
                cancellationToken);

            return NoContent();
        }


        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateRoomStatus(
     int id,
     [FromBody] UpdateRoomStatusRequest updateRoomStatusRequest,
     CancellationToken cancellationToken)
        {
            await _roomService.UpdateRoomStatusAsync(
                id,
                updateRoomStatusRequest,
                cancellationToken);

            return NoContent();
        }
    }
}
