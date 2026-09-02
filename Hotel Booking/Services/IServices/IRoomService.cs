namespace Hotel_Booking.Services.IServices
{
    public interface IRoomService
    {
        Task<PagedResponse<RoomResponse>> GetAvailableRoomsAsync(
            int page,
            CancellationToken cancellationToken);

        Task<PagedResponse<RoomResponse>> GetAllRoomsAsync(
            int page,
            CancellationToken cancellationToken);

        Task<RoomResponse> GetRoomByIdAsync(
            int id,
            CancellationToken cancellationToken);

        Task UpdateRoomStatusAsync(
    int id,
    UpdateRoomStatusRequest updateRoomStatusRequest,
    CancellationToken cancellationToken);

        Task CreateRoomAsync(
      CreateRoomRequest createRoomRequest,
          List<IFormFile> images,
      CancellationToken cancellationToken);

        Task UpdateRoomAsync(
            int id,
            UpdateRoomRequest  updateRoomRequest,
             List<IFormFile> images,
            CancellationToken cancellationToken);
    }
}