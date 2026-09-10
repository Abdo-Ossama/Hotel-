using Hotel_Booking.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Hotel_Booking.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRepository<Room> _roomsRepository;
        private readonly ICacheService _cacheService;
        private readonly IImageService _imageService;
        private readonly ILogger<RoomService> _logger;

        public RoomService(IRepository<Room> roomsRepository,
            ICacheService cacheService,
            IImageService imageService,
            ILogger<RoomService> logger)
        {
            _roomsRepository = roomsRepository;
            _cacheService = cacheService;
            _imageService = imageService;
            _logger = logger;
        }



        // Get Available Rooms
        public async Task<PagedResponse<RoomResponse>> GetAvailableRoomsAsync(
            int page,
            CancellationToken cancellationToken)
        {
            const int pageSize = 5;

            if (page <= 0)
            {
                throw new ValidationAppException(
                    "Invalid page number.",
                    new Dictionary<string, string[]>
                    {
                        ["Page"] =
                        [
                            "Page number must be greater than zero."
                        ]
                    });
            }

            // Cache Key
            var cacheKey = $"rooms:available:page:{page}";

            // Get From Cache
            var cachedRooms =
                await _cacheService.GetAsync<PagedResponse<RoomResponse>>(
                    cacheKey);

            if (cachedRooms is not null)
            {
                _logger.LogInformation("Cahce Hit {cacheKey}", cacheKey);
                return cachedRooms;
            }

            // Get From Database
            var roomqQuery = _roomsRepository.GetQueryable(
                  includes:
                  [
                      e => e.RoomType,
                    e => e.RoomImages
                  ],
                  tracked: false);
            roomqQuery = roomqQuery
                .Include(e => e.RoomAmenities)
                .ThenInclude(e => e.Amenity);


            var totalRooms = await roomqQuery.CountAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling(
                totalRooms / (double)pageSize);

            var rooms = roomqQuery
                .OrderBy(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToRoomResponse)
                .ToList();

            var response = new PagedResponse<RoomResponse>
            {
                Items = rooms,
                CurrentPage = page,
                HasPrevious = page - 1,
                HasNext = page + 1,
                PageSize = pageSize,
                TotalCount = totalRooms,
                TotalPages = totalPages
            };

            // Store In Cache
            await _cacheService.SetAsync(
                cacheKey,
                response,
                TimeSpan.FromMinutes(10));

            return response;
        }


        // Get All Rooms
        public async Task<PagedResponse<RoomResponse>> GetAllRoomsAsync(
            int page,
            CancellationToken cancellationToken)
        {
            const int pageSize = 5;

            if (page <= 0)
            {
                throw new ValidationAppException(
                    "Invalid page number.",
                    new Dictionary<string, string[]>
                    {
                        ["Page"] =
                        [
                            "Page number must be greater than zero."
                        ]
                    });
            }


            var roomqQuery = _roomsRepository.GetQueryable(
                includes:
                [
                    e => e.RoomType,
                    e => e.RoomImages
                ],
                tracked: false);
            roomqQuery = roomqQuery
                .Include(e => e.RoomAmenities)
                .ThenInclude(e => e.Amenity);

 
            var totalRooms = await roomqQuery.CountAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling(
                totalRooms / (double)pageSize);

            var rooms = roomqQuery
                .OrderBy(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToRoomResponse)
                .ToList();

            return new PagedResponse<RoomResponse>
            {
                Items = rooms,
                CurrentPage = page,
                HasPrevious = page -1,
                HasNext = page + 1,
                PageSize = pageSize,
                TotalCount = totalRooms,
                TotalPages = totalPages
            };
        }


        // Get Room By Id
        public async Task<RoomResponse> GetRoomByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                throw new ValidationAppException(
                    "Invalid Id.",
                    new Dictionary<string, string[]>
                    {
                        ["Id"] =
                        [
                            "Room ID is not found."
                        ]
                    });
            }

            var room = await _roomsRepository.GetOneAsync(
                e => e.Id == id,

                includes:
                [
                    e => e.RoomImages,
                    e => e.RoomType
                ],

                tracked: false,

                includeThen: e => e
                    .Include(e => e.RoomAmenities)
                    .ThenInclude(e => e.Amenity),

                cancellationToken: cancellationToken
            );

            if (room is null)
            {
                throw new NotFoundException("Room is not found.");
            }

            return MapToRoomResponse(room);
        }



        public async Task UpdateRoomStatusAsync(
    int id,
    UpdateRoomStatusRequest updateRoomStatusRequest,
    CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                throw new ValidationAppException(
                    "Invalid Id.",
                    new Dictionary<string, string[]>
                    {
                        ["Id"] =
                        [
                            "Room ID must be greater than zero."
                        ]
                    });
            }

            if (!updateRoomStatusRequest.Status.HasValue ||
                !Enum.IsDefined(
                    typeof(RoomStatus),
                    updateRoomStatusRequest.Status.Value))
            {
                throw new ValidationAppException(
                    "Invalid room status.",
                    new Dictionary<string, string[]>
                    {
                        ["Status"] =
                        [
                            "The provided room status is invalid."
                        ]
                    });
            }

            var room = await _roomsRepository.GetOneAsync(
                e => e.Id == id,
                tracked: true,
                cancellationToken: cancellationToken);

            if (room is null)
            {
                throw new NotFoundException(
                    "Room is not found.");
            }

            var oldStatus = room.Status;
            var newStatus = updateRoomStatusRequest.Status.Value;

            if (oldStatus == newStatus)
            {
                _logger.LogInformation(
                    "Room {RoomId} already has status {Status}.",
                    id,
                    newStatus);

                return;
            }

            room.Status = newStatus;

            await _roomsRepository.CommitAsync(
                cancellationToken);

            // Invalidate available rooms cache
            await _cacheService.RemoveByPatternAsync(
                "rooms:available:page:*");

            _logger.LogInformation(
                "Room {RoomId} status changed from {OldStatus} to {NewStatus}.",
                id,
                oldStatus,
                newStatus);
        }




        // Create Room
        public async Task CreateRoomAsync(
     CreateRoomRequest  createRoomRequest,
       List<IFormFile> images,
     CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(createRoomRequest.RoomNumber))
            {
                throw new ValidationAppException(
                    "Invalid room data.",
                    new Dictionary<string, string[]>
                    {
                        ["RoomNumber"] =
                        [
                            "Room number is required."
                        ]
                    });
            }

            if (createRoomRequest.Floor <= 0)
            {
                throw new ValidationAppException(
                    "Invalid room data.",
                    new Dictionary<string, string[]>
                    {
                        ["Floor"] =
                        [
                            "Floor must be greater than zero."
                        ]
                    });
            }

            if (createRoomRequest.RoomTypeId <= 0)
            {
                throw new ValidationAppException(
                    "Invalid room data.",
                    new Dictionary<string, string[]>
                    {
                        ["RoomTypeId"] =
                        [
                            "Room type is required."
                        ]
                    });
            }

            var room = new Room
            {
                RoomNumber = createRoomRequest.RoomNumber.Trim(), // delete spaces
                Floor = createRoomRequest.Floor,
                RoomTypeId = createRoomRequest.RoomTypeId
            };

            // Add Amenities
            if (createRoomRequest.AmenityIds.Any())
            {
                room.RoomAmenities = createRoomRequest.AmenityIds
                    .Distinct()
                    .Select(amenityId => new RoomAmenity
                    {
                        AmenityId = amenityId
                    })
                    .ToList();
            }

            // Upload Images
            if (images.Any())
            {
                foreach (var image in images)
                {
                    var fileName = await _imageService.UploadImageAsync(
                        image,
                        "rooms_img");

                    room.RoomImages.Add(new RoomImage
                    {
                        Url = fileName
                    });
                }
            }

            await _roomsRepository.CreateAysnc(
                room,
                cancellationToken);

            await _roomsRepository.CommitAsync(
                cancellationToken);

            // Invalidate available rooms cache
            await _cacheService.RemoveByPatternAsync(
                "rooms:available:page:*");

            _logger.LogInformation(
                "Room {RoomId} created successfully.",
                room.Id);
        }

        //update room

        public async Task UpdateRoomAsync(
    int id,
    UpdateRoomRequest updateRoomRequest,
    List<IFormFile> images,
    CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                throw new ValidationAppException(
                    "Invalid Id.",
                    new Dictionary<string, string[]>
                    {
                        ["Id"] =
                        [
                            "Room ID must be greater than zero."
                        ]
                    });
            }


            if (updateRoomRequest.Floor <= 0)
            {
                throw new ValidationAppException(
                    "Invalid room data.",
                    new Dictionary<string, string[]>
                    {
                        ["Floor"] =
                        [
                            "Floor must be greater than zero."
                        ]
                    });
            }

            if (updateRoomRequest.RoomTypeId <= 0)
            {
                throw new ValidationAppException(
                    "Invalid room data.",
                    new Dictionary<string, string[]>
                    {
                        ["RoomTypeId"] =
                        [
                            "Room type is required."
                        ]
                    });
            }

            var room = await _roomsRepository.GetOneAsync(
                e => e.Id == id,

                includes:
                [
                    e => e.RoomImages,
                    e => e.RoomType
                ],

                tracked: false,

                includeThen: e => e
                    .Include(e => e.RoomAmenities)
                    .ThenInclude(e => e.Amenity),

                cancellationToken: cancellationToken
            );

            if (room is null)
            {
                throw new NotFoundException("Room is not found.");
            }

            // Update basic information
            room.RoomNumber = updateRoomRequest.RoomNumber.Trim()??room.RoomNumber;
            room.Floor =updateRoomRequest.Floor ?? room.Floor;
            room.RoomTypeId =updateRoomRequest.RoomTypeId ?? room.RoomTypeId;

            if(updateRoomRequest.AmenityIds is not null)
            {
                if (updateRoomRequest.AmenityIds.Any())
                {
                    room.RoomAmenities = updateRoomRequest.AmenityIds
                        .Distinct()
                        .Select(amenityId => new RoomAmenity
                        {
                            RoomId = room.Id,
                            AmenityId = amenityId
                        })
                        .ToList();
                }
            }

            // Update Images
            if (images.Any())
            {
                // Delete old images
                foreach (var oldImage in room.RoomImages)
                {
                    if (!string.IsNullOrEmpty(oldImage.Url))
                    {
                        _imageService.DeleteImage(
                            oldImage.Url,
                            "rooms_img");
                    }
                }

                // Remove old images from database
                room.RoomImages.Clear();

                // Upload new images
                foreach (var image in images)
                {
                    var fileName =
                        await _imageService.UploadImageAsync(
                            image,
                            "rooms_img");

                    room.RoomImages.Add(
                        new RoomImage
                        {
                            Url = fileName
                        });
                }
            }


            // Update database
            _roomsRepository.Update(room);

            await _roomsRepository.CommitAsync(
                cancellationToken);


            // Invalidate available rooms cache
            await _cacheService.RemoveByPatternAsync(
                "rooms:available:page:*");

            // Remove room details cache
            await _cacheService.RemoveAsync(
                $"room:{id}");


            _logger.LogInformation(
                "Room {RoomId} updated successfully.",
                id);
        }









        // Response of Get - update Room ..
        //بدلا من اني مع كل اكشن هعمل سيليكت وارجع نفس الداتا 
        private static RoomResponse MapToRoomResponse(Room room)
        {
            return new RoomResponse
            {
                Id = room.Id,
                RoomNumber = room.RoomNumber,
                Floor = room.Floor,
                Status = room.Status,

                RoomType = new RoomTypeResponse
                {
                    Id = room.RoomType.Id,
                    Name = room.RoomType.Name,
                    Description = room.RoomType.Description,
                    BasePricePerNight =
                        room.RoomType.BasePricePerNight,
                    MaxAdults = room.RoomType.MaxAdults,
                    MaxChildren = room.RoomType.MaxChildren
                },

                Amenities = room.RoomAmenities
                    .Select(e => new AmenityResponse
                    {
                        Id = e.Amenity.Id,
                        Name = e.Amenity.Name,
                        Icon = e.Amenity.Icon
                    })
                    .ToList(),

                Images = room.RoomImages
                    .Select(e => new RoomImageResponse
                    {
                        Id = e.Id,
                        ImageUrl = e.Url
                    })
                    .ToList()
            };
        }
    }
}



