
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Hotel_Booking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
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

        //get room by id

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
    }
}
