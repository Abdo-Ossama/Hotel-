using Hotel_Booking;
using Hotel_Booking.Enums;
using Hotel_Booking.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;


public class BookingService : IBookingService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<Room> _roomRepository;
    private readonly IRepository<Refund> _refundRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BookingService> _logger;

    public BookingService(UserManager<ApplicationUser> userManager, IRepository<Booking> bookingRepository, IRepository<Room> roomRepository, IRepository<Refund> refundRepository, IUnitOfWork unitOfWork, ILogger<BookingService> logger)
    {
        _userManager = userManager;
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _refundRepository = refundRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BookingResponse> CreateBookingAsync(
        CreateBookingRequest  createBookingRequest,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException(
                "User not authenticated.");

        if (createBookingRequest is null)
        {
            throw new ValidationAppException(
                "Invalid booking request.",
                new Dictionary<string, string[]>
                {
                    ["Request"] =
                    [
                        "Booking request is required."
                    ]
                });
        }

        if (string.IsNullOrWhiteSpace(createBookingRequest.GuestId))
        {
            throw new ValidationAppException(
                "Invalid customer.",
                new Dictionary<string, string[]>
                {
                    ["GuestId"] =
                    [
                        "Guest ID is required."
                    ]
                });
        }

        var guest = await _userManager.FindByIdAsync(
            createBookingRequest.GuestId);

        if (guest is null)
        {
            throw new NotFoundException(
                "Guest was not found.");
        }

        var isGuest = await _userManager.IsInRoleAsync(
            guest,
            "Guest");

        if (!isGuest)
        {
            throw new ValidationAppException(
                "Invalid customer.",
                new Dictionary<string, string[]>
                {
                    ["CustomerId"] =
                    [
                        "The selected customer is not a Guest."
                    ]
                });
        }

        if (createBookingRequest.RoomIds == null ||
            !createBookingRequest.RoomIds.Any() ||
            createBookingRequest.RoomIds.Any(id => id <= 0))
        {
            throw new ValidationAppException(
                "Invalid Room IDs.",
                new Dictionary<string, string[]>
                {
                    ["RoomIds"] =
                    [
                        "At least one room must be selected, and all Room IDs must be greater than zero."
                    ]
                });
        }

        if (createBookingRequest.GuestCount <= 0)
        {
            throw new ValidationAppException(
                "Invalid Guest Count.",
                new Dictionary<string, string[]>
                {
                    ["GuestCount"] =
                    [
                        "Guest count must be greater than zero."
                    ]
                });
        }

        var checkIn = createBookingRequest.CheckIn.Date;
        var checkOut = createBookingRequest.CheckOut.Date;
        var today = DateTime.UtcNow.Date;

        if (checkIn < today)
        {
            throw new ValidationAppException(
                "Invalid check-in date.",
                new Dictionary<string, string[]>
                {
                    ["CheckIn"] =
                    [
                        "Check-in date cannot be in the past."
                    ]
                });
        }

        if (checkOut <= checkIn)
        {
            throw new ValidationAppException(
                "Invalid check-out date.",
                new Dictionary<string, string[]>
                {
                    ["CheckOut"] =
                    [
                        "Check-out date must be after check-in date."
                    ]
                });
        }

        if (createBookingRequest.Guests != null &&
            createBookingRequest.Guests.Count > 0 &&
            createBookingRequest.Guests.Count != createBookingRequest.GuestCount)
        {
            throw new ValidationAppException(
                "Invalid guest information.",
                new Dictionary<string, string[]>
                {
                    ["Guests"] =
                    [
                        "The number of guest records must match GuestCount."
                    ]
                });
        }

        var totalNights = (checkOut - checkIn).Days;

        var roomIds = createBookingRequest.RoomIds
            .Distinct()
            .ToList();

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            await ExpirePendingBookingsAsync(
                roomIds,
                cancellationToken);

            var rooms = (await _roomRepository.GetAsync(
                e => roomIds.Contains(e.Id),
                includes: [e => e.RoomType],
                tracked: true,
                cancellationToken: cancellationToken))
                .ToList();

            if (rooms.Count == 0)
            {
                throw new NotFoundException(
                    "Room is not found.");
            }

            if (rooms.Count != roomIds.Count)
            {
                var missingIds = roomIds
                    .Except(rooms.Select(e => e.Id))
                    .ToList();

                throw new NotFoundException(
                    $"The following room(s) were not found: {string.Join(", ", missingIds)}.");
            }

            foreach (var room in rooms)
            {
                if (room.Status != RoomStatus.Available)
                {
                    throw new ConflictException(
                        $"Room {room.RoomNumber} is currently {room.Status} and cannot be reserved.");
                }
            }

            var totalCapacity = rooms.Sum(
                e => e.RoomType.Capacity);

            if (createBookingRequest.GuestCount > totalCapacity)
            {
                throw new ValidationAppException(
                    "Invalid Guest Count.",
                    new Dictionary<string, string[]>
                    {
                        ["GuestCount"] =
                        [
                            $"Guest count must be between 1 and {totalCapacity}."
                        ]
                    });
            }

            var hasOverlap =
                await _bookingRepository.AnyAsync(
                    e =>
                        e.BookingRooms.Any(
                            br => roomIds.Contains(br.RoomId)) &&
                        e.Status != BookingStatus.Cancelled &&
                        e.Status != BookingStatus.Expired &&
                        checkIn < e.CheckOutDate &&
                        checkOut > e.CheckInDate,
                    cancellationToken: cancellationToken);

            if (hasOverlap)
            {
                throw new ConflictException(
                    "One or more selected rooms are not available for the selected dates.");
            }

            var now = DateTime.UtcNow;

            foreach (var room in rooms)
            {
                room.Status = RoomStatus.Reserved;
                room.UpdatedAtUtc = now;
            }

            var basePrice =
                rooms.Sum(
                    r => r.RoomType.BasePricePerNight)
                * totalNights;

            var taxes = basePrice * 0.14m;
            var total = basePrice + taxes;

            var booking = new Booking
            {
                BookingNumber = GenerateBookingNumber(),

                CustomerId = createBookingRequest.GuestId,

                CheckInDate = checkIn,
                CheckOutDate = checkOut,

                TotalNights = totalNights,
                GuestCount = createBookingRequest.GuestCount,

                TaxAmount = taxes,
                TotalPrice = total,

                Status = BookingStatus.PendingPayment,

                PaymentDueAtUtc =
                    now.AddMinutes(30),

                CreatedAtUtc = now,
                UpdatedAtUtc = now,

                BookingRooms = rooms
                    .Select(r => new BookingRoom
                    {
                        RoomId = r.Id,

                        PricePerNightSnapshot =
                            r.RoomType.BasePricePerNight,

                        RoomNumberSnapshot =
                            r.RoomNumber,

                        RoomTypeNameSnapshot =
                            r.RoomType.Name
                    })
                    .ToList(),

                Guests = createBookingRequest.Guests?
                    .Select(g => new Guest
                    {
                        FirstName = g.FirstName?.Trim()!,
                        LastName = g.LastName?.Trim()!,
                        Email = g.Email?.Trim(),
                        Phone = g.Phone?.Trim()
                    })
                    .ToList()
                    ?? new List<Guest>()
            };

            await _bookingRepository.CreateAysnc(
                booking,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            _logger.LogInformation(
                "Booking created successfully. BookingId: {BookingId}, BookingNumber: {BookingNumber}, UserId: {UserId}",
                booking.Id,
                booking.BookingNumber,
                userId);

            return new BookingResponse
            {
                Id = booking.Id,

                BookingNumber =
                    booking.BookingNumber,

                RoomIds =
                    booking.BookingRooms
                        .Select(br => br.RoomId)
                        .ToList(),

                CheckIn =
                    booking.CheckInDate,

                CheckOut =
                    booking.CheckOutDate,

                GuestCount =
                    (int)booking.GuestCount,

                TotalNights =
                    booking.TotalNights,

                TotalPrice =
                    booking.TotalPrice,

                Status =
                    booking.Status.ToString(),

                PaymentDueAtUtc =
                    booking.PaymentDueAtUtc!.Value
            };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            _logger.LogWarning(
                ex,
                "Concurrency conflict while creating booking for UserId: {UserId}",
                userId);

            throw new ConflictException(
                "One or more selected rooms were modified by another operation. Please try again.");
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private async Task ExpirePendingBookingsAsync(
        List<int> roomIds,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var expiredBookings = await _bookingRepository
            .GetQueryable(
                e =>
                    e.Status == BookingStatus.PendingPayment &&
                    e.PaymentDueAtUtc.HasValue &&
                    e.PaymentDueAtUtc.Value <= now &&
                    e.BookingRooms.Any(
                        br => roomIds.Contains(br.RoomId)),
                includes:
                [
                    e => e.BookingRooms
                ],
                tracked: true)
            .ToListAsync(cancellationToken);

        if (expiredBookings.Count == 0)
            return;

        var expiredRoomIds = expiredBookings
            .SelectMany(
                b => b.BookingRooms.Select(
                    br => br.RoomId))
            .Distinct()
            .ToList();

        foreach (var booking in expiredBookings)
        {
            booking.Status = BookingStatus.Expired;
            booking.UpdatedAtUtc = now;
        }

        if (expiredRoomIds.Count > 0)
        {
            var rooms = await _roomRepository.GetAsync(
                e => expiredRoomIds.Contains(e.Id),
                tracked: true,
                cancellationToken: cancellationToken);

            foreach (var room in rooms)
            {
                if (room.Status == RoomStatus.Reserved)
                {
                    room.Status = RoomStatus.Available;
                    room.UpdatedAtUtc = now;
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        foreach (var booking in expiredBookings)
        {
            _logger.LogInformation(
                "Pending booking expired. BookingId: {BookingId}, BookingNumber: {BookingNumber}",
                booking.Id,
                booking.BookingNumber);
        }
    }

    public async Task CancelBookingAsync(
        Guid bookingId,
        string userId,
        string? cancellationReason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException(
                "User not authenticated.");
        }

        if (bookingId == Guid.Empty)
        {
            throw new ValidationAppException(
                "Invalid booking id.",
                new Dictionary<string, string[]>
                {
                    ["BookingId"] =
                    [
                        "Booking id is required."
                    ]
                });
        }

        cancellationReason =
            cancellationReason?.Trim();

        if (!string.IsNullOrWhiteSpace(cancellationReason) &&
            cancellationReason.Length > 500)
        {
            throw new ValidationAppException(
                "Invalid cancellation reason.",
                new Dictionary<string, string[]>
                {
                    ["CancellationReason"] =
                    [
                        "Cancellation reason cannot exceed 500 characters."
                    ]
                });
        }

        var now = DateTime.UtcNow;

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        try
        {
            var booking = await _bookingRepository
                .GetQueryable(
                    e =>
                        e.Id == bookingId &&
                        e.CustomerId == userId,
                    includes:
                    [
                        e => e.Payments,
                        e => e.BookingRooms
                    ],
                    tracked: true)
                .FirstOrDefaultAsync(
                    cancellationToken);

            if (booking is null)
                throw new NotFoundException(
                    "Booking not found.");

            if (booking.Status == BookingStatus.Cancelled)
            {
                throw new ValidationAppException(
                    "Booking is already cancelled.",
                    new Dictionary<string, string[]>
                    {
                        ["Booking"] =
                        [
                            "This booking has already been cancelled."
                        ]
                    });
            }

            if (booking.Status is not
                (BookingStatus.PendingPayment or BookingStatus.Confirmed))
            {
                throw new ValidationAppException(
                    "Booking cannot be cancelled in its current status.",
                    new Dictionary<string, string[]>
                    {
                        ["Status"] =
                        [
                            "The current booking status does not allow cancellation."
                        ]
                    });
            }

            if (now.Date >= booking.CheckInDate.Date)
            {
                throw new ValidationAppException(
                    "Booking cannot be cancelled on or after check-in date.",
                    new Dictionary<string, string[]>
                    {
                        ["CheckInDate"] =
                        [
                            "The booking cannot be cancelled on or after the check-in date."
                        ]
                    });
            }

            booking.Status =
                BookingStatus.Cancelled;

            booking.CancelledAtUtc = now;
            booking.UpdatedAtUtc = now;

            booking.CancellationReason =
                string.IsNullOrWhiteSpace(cancellationReason)
                    ? null
                    : cancellationReason;

            var roomIds = booking.BookingRooms
                .Select(br => br.RoomId)
                .Distinct()
                .ToList();

            if (roomIds.Count > 0)
            {
                var rooms = (await _roomRepository.GetAsync(
                    e => roomIds.Contains(e.Id),
                    tracked: true,
                    cancellationToken: cancellationToken))
                    .ToList();

                foreach (var room in rooms)
                {
                    if (room.Status == RoomStatus.Reserved)
                    {
                        room.Status =
                            RoomStatus.Available;

                        room.UpdatedAtUtc = now;
                    }
                }
            }

            var paidPayments = booking.Payments
                .Where(p => p.Status == PaymentStatus.Paid)
                .OrderBy(p => p.CreatedAtUtc)
                .ToList();

            if (paidPayments.Count > 0)
            {
                var hoursUntilCheckIn =
                    (booking.CheckInDate - now).TotalHours;

                decimal refundPercentage =
                    hoursUntilCheckIn >=
                    CancellationPolicy.FullRefundHours
                        ? 1m
                        : hoursUntilCheckIn >=
                          CancellationPolicy.PartialRefundHours
                            ? CancellationPolicy.PartialRefundPercentage
                            : 0m;

                foreach (var payment in paidPayments)
                {
                    if (refundPercentage <= 0)
                        continue;

                    var refundAlreadyExists =
                        await _refundRepository.AnyAsync(
                            e =>
                                e.PaymentId == payment.Id &&
                                e.Status != RefundStatus.Failed,
                            cancellationToken);

                    if (refundAlreadyExists)
                    {
                        _logger.LogWarning(
                            "Refund already exists for PaymentId: {PaymentId}, BookingId: {BookingId}.",
                            payment.Id,
                            booking.Id);

                        continue;
                    }

                    var refundAmount =
                        Math.Min(
                            payment.Amount * refundPercentage,
                            payment.Amount);

                    if (refundAmount <= 0)
                        continue;

                    var refund = new Refund
                    {
                        Id = Guid.NewGuid(),

                        PaymentId =
                            payment.Id,

                        Amount =
                            refundAmount,

                        Currency =
                            payment.Currency,

                        Reason =
                            cancellationReason ??
                            "Customer cancelled the booking.",

                        Status =
                            RefundStatus.RefundPending,

                        CreatedAtUtc =
                            now
                    };

                    await _refundRepository.CreateAysnc(
                        refund,
                        cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            _logger.LogInformation(
                "Booking cancelled successfully. BookingId: {BookingId}, BookingNumber: {BookingNumber}, UserId: {UserId}",
                booking.Id,
                booking.BookingNumber,
                userId);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            _logger.LogWarning(
                ex,
                "Concurrency conflict on booking {BookingId}.",
                bookingId);

            throw new ConflictException(
                "This booking was modified by another operation. Please refresh and try again.");
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task CheckInAsync(
        Guid bookingId,
        string staffUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(staffUserId))
        {
            throw new ValidationAppException(
                "Invalid staff user.",
                new Dictionary<string, string[]>
                {
                    ["StaffUserId"] =
                    [
                        "Staff user id is required."
                    ]
                });
        }

        if (bookingId == Guid.Empty)
        {
            throw new ValidationAppException(
                "Invalid booking id.",
                new Dictionary<string, string[]>
                {
                    ["BookingId"] =
                    [
                        "Booking id is required."
                    ]
                });
        }

        var now = DateTime.UtcNow;

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        try
        {
            var booking = await _bookingRepository
                .GetQueryable(
                    e => e.Id == bookingId,
                    includes:
                    [
                        e => e.BookingRooms
                    ],
                    tracked: true)
                .FirstOrDefaultAsync(
                    cancellationToken);

            if (booking is null)
                throw new NotFoundException(
                    "Booking not found.");

            if (booking.Status != BookingStatus.Confirmed)
            {
                throw new ValidationAppException(
                    "Booking cannot be checked in from its current status.",
                    new Dictionary<string, string[]>
                    {
                        ["Status"] =
                        [
                            $"Only confirmed bookings can be checked in. Current status: {booking.Status}."
                        ]
                    });
            }

            if (booking.CheckedInAtUtc is not null)
            {
                throw new ValidationAppException(
                    "Guest is already checked in.",
                    new Dictionary<string, string[]>
                    {
                        ["CheckIn"] =
                        [
                            "This booking has already been checked in."
                        ]
                    });
            }

            if (now.Date < booking.CheckInDate.Date)
            {
                throw new ValidationAppException(
                    "Check-in date has not arrived yet.",
                    new Dictionary<string, string[]>
                    {
                        ["CheckInDate"] =
                        [
                            $"Check-in is only allowed on or after {booking.CheckInDate:yyyy-MM-dd}."
                        ]
                    });
            }

            if (now.Date >= booking.CheckOutDate.Date)
            {
                throw new ValidationAppException(
                    "Booking period has already ended.",
                    new Dictionary<string, string[]>
                    {
                        ["CheckOutDate"] =
                        [
                            "This booking's stay period has already ended and cannot be checked in."
                        ]
                    });
            }

            var roomIds = booking.BookingRooms
                .Select(br => br.RoomId)
                .Distinct()
                .ToList();

            if (roomIds.Count == 0)
            {
                throw new ValidationAppException(
                    "Booking has no rooms assigned.",
                    new Dictionary<string, string[]>
                    {
                        ["Rooms"] =
                        [
                            "At least one room must be assigned to the booking."
                        ]
                    });
            }

            var rooms = (await _roomRepository.GetAsync(
                e => roomIds.Contains(e.Id),
                tracked: true,
                cancellationToken: cancellationToken))
                .ToList();

            if (rooms.Count != roomIds.Count)
            {
                throw new ValidationAppException(
                    "One or more rooms could not be found.",
                    new Dictionary<string, string[]>
                    {
                        ["Rooms"] =
                        [
                            "One or more rooms associated with this booking do not exist."
                        ]
                    });
            }

            foreach (var room in rooms)
            {
                if (room.Status != RoomStatus.Reserved)
                {
                    throw new ValidationAppException(
                        "Room is not reserved for this booking.",
                        new Dictionary<string, string[]>
                        {
                            ["Room"] =
                            [
                                $"Room {room.RoomNumber} is currently {room.Status}. Expected: Reserved."
                            ]
                        });
                }
            }

            booking.Status =
                BookingStatus.CheckedIn;

            booking.CheckedInAtUtc = now;
            booking.CheckedInByUserId = staffUserId;
            booking.UpdatedAtUtc = now;

            foreach (var room in rooms)
            {
                room.Status =
                    RoomStatus.Occupied;

                room.UpdatedAtUtc = now;
            }

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            _logger.LogInformation(
                "Guest checked in successfully. BookingId: {BookingId}, BookingNumber: {BookingNumber}, StaffUserId: {StaffUserId}",
                booking.Id,
                booking.BookingNumber,
                staffUserId);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            _logger.LogWarning(
                ex,
                "Concurrency conflict on booking {BookingId} during check-in.",
                bookingId);

            throw new ConflictException(
                "This booking or one of its rooms was modified by another operation. Please refresh and try again.");
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task CheckOutAsync(
        Guid bookingId,
        string staffUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(staffUserId))
        {
            throw new ValidationAppException(
                "Invalid staff user.",
                new Dictionary<string, string[]>
                {
                    ["StaffUserId"] =
                    [
                        "Staff user id is required."
                    ]
                });
        }

        if (bookingId == Guid.Empty)
        {
            throw new ValidationAppException(
                "Invalid booking id.",
                new Dictionary<string, string[]>
                {
                    ["BookingId"] =
                    [
                        "Booking id is required."
                    ]
                });
        }

        var now = DateTime.UtcNow;

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        try
        {
            var booking = await _bookingRepository
                .GetQueryable(
                    e => e.Id == bookingId,
                    includes:
                    [
                        e => e.BookingRooms
                    ],
                    tracked: true)
                .FirstOrDefaultAsync(
                    cancellationToken);

            if (booking is null)
                throw new NotFoundException(
                    "Booking not found.");

            if (booking.Status != BookingStatus.CheckedIn)
            {
                throw new ValidationAppException(
                    "Booking cannot be checked out from its current status.",
                    new Dictionary<string, string[]>
                    {
                        ["Status"] =
                        [
                            $"Only checked-in bookings can be checked out. Current status: {booking.Status}."
                        ]
                    });
            }

            if (booking.CheckedOutAtUtc is not null)
            {
                throw new ValidationAppException(
                    "Guest is already checked out.",
                    new Dictionary<string, string[]>
                    {
                        ["CheckOut"] =
                        [
                            "This booking has already been checked out."
                        ]
                    });
            }

            var roomIds = booking.BookingRooms
                .Select(br => br.RoomId)
                .Distinct()
                .ToList();

            if (roomIds.Count == 0)
            {
                throw new ValidationAppException(
                    "Booking has no rooms assigned.",
                    new Dictionary<string, string[]>
                    {
                        ["Rooms"] =
                        [
                            "At least one room must be assigned to the booking."
                        ]
                    });
            }

            var rooms = (await _roomRepository.GetAsync(
                e => roomIds.Contains(e.Id),
                tracked: true,
                cancellationToken: cancellationToken))
                .ToList();

            if (rooms.Count != roomIds.Count)
            {
                throw new ValidationAppException(
                    "One or more rooms could not be found.",
                    new Dictionary<string, string[]>
                    {
                        ["Rooms"] =
                        [
                            "One or more rooms associated with this booking do not exist."
                        ]
                    });
            }

            foreach (var room in rooms)
            {
                if (room.Status != RoomStatus.Occupied)
                {
                    throw new ValidationAppException(
                        "Room is not occupied.",
                        new Dictionary<string, string[]>
                        {
                            ["Room"] =
                            [
                                $"Room {room.RoomNumber} is currently {room.Status}. Expected: Occupied."
                            ]
                        });
                }
            }

            booking.Status =
                BookingStatus.CheckedOut;

            booking.CheckedOutAtUtc = now;
            booking.CheckedOutByUserId = staffUserId;
            booking.UpdatedAtUtc = now;

            foreach (var room in rooms)
            {
                room.Status =
                    RoomStatus.Cleaning;

                room.UpdatedAtUtc = now;
            }

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            _logger.LogInformation(
                "Guest checked out successfully. BookingId: {BookingId}, BookingNumber: {BookingNumber}, StaffUserId: {StaffUserId}",
                booking.Id,
                booking.BookingNumber,
                staffUserId);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            _logger.LogWarning(
                ex,
                "Concurrency conflict on booking {BookingId} during check-out.",
                bookingId);

            throw new ConflictException(
                "This booking or one of its rooms was modified by another operation. Please refresh and try again.");
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task<BookingDetailsResponse> GetBookingByNumberAsync(
        string bookingNumber,
        string userId,
        bool isStaff,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException(
                "User not authenticated.");

        if (string.IsNullOrWhiteSpace(bookingNumber))
        {
            throw new ValidationAppException(
                "Invalid booking number.",
                new Dictionary<string, string[]>
                {
                    ["BookingNumber"] =
                    [
                        "Booking number is required."
                    ]
                });
        }

        bookingNumber =
            bookingNumber.Trim();

        var booking = await _bookingRepository
            .GetQueryable(
                e =>
                    e.BookingNumber == bookingNumber &&
                    (e.CustomerId == userId || isStaff),
                tracked: false)
            .Select(e => new BookingDetailsResponse
            {
                Id = e.Id,

                BookingNumber =
                    e.BookingNumber,

                CheckIn =
                    e.CheckInDate,

                CheckOut =
                    e.CheckOutDate,

                TotalNights =
                    e.TotalNights,

                GuestCount =
                    (int)e.GuestCount,

                TaxAmount =
                    e.TaxAmount,

                TotalPrice =
                    e.TotalPrice,

                Status =
                    e.Status.ToString(),

                PaymentStatus =
                    e.Payments
                        .OrderByDescending(
                            p => p.CreatedAtUtc)
                        .Select(
                            p => p.Status.ToString())
                        .FirstOrDefault()
                    ?? "No Payment",

                PaymentDueAtUtc =
                    (DateTime)e.PaymentDueAtUtc!,

                CreatedAt =
                    e.CreatedAtUtc,

                Rooms =
                    e.BookingRooms
                        .Select(br => new BookingRoomResponse
                        {
                            RoomId =
                                br.RoomId,

                            RoomNumber =
                                br.RoomNumberSnapshot,

                            RoomTypeName =
                                br.RoomTypeNameSnapshot,

                            PricePerNight =
                                br.PricePerNightSnapshot
                        })
                        .ToList(),

                Guests =
                    e.Guests
                        .Select(g => new BookingGuestResponse
                        {
                            FirstName =
                                g.FirstName,

                            LastName =
                                g.LastName,

                            Email =
                                g.Email!,

                            Phone =
                                g.Phone!
                        })
                        .ToList()
            })
            .FirstOrDefaultAsync(
                cancellationToken);

        if (booking is null)
            throw new NotFoundException(
                "Booking not found.");

        return booking;
    }

    public async Task<PagedResponse<MyBookingResponse>> GetMyBookingsAsync(
        string userId,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException(
                "User not authenticated.");

        if (page <= 0)
            page = 1;

        const int pageSize = 5;

        var bookingQuery =
            _bookingRepository.GetQueryable(
                e => e.CustomerId == userId,
                tracked: false);

        var totalCount =
            await bookingQuery.CountAsync(
                cancellationToken);

        var bookings =
            await bookingQuery
                .OrderByDescending(
                    e => e.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new MyBookingResponse
                {
                    Id = e.Id,

                    BookingNumber =
                        e.BookingNumber,

                    CheckIn =
                        e.CheckInDate,

                    CheckOut =
                        e.CheckOutDate,

                    TotalNights =
                        e.TotalNights,

                    TotalPrice =
                        e.TotalPrice,

                    Status =
                        e.Status.ToString(),

                    PaymentStatus =
                        e.Payments
                            .OrderByDescending(
                                p => p.CreatedAtUtc)
                            .Select(
                                p => p.Status.ToString())
                            .FirstOrDefault()
                        ?? "No Payment",

                    RoomTypeName =
                        e.BookingRooms
                            .Select(
                                br => br.RoomTypeNameSnapshot)
                            .FirstOrDefault()
                        ?? "N/A",

                    CreatedAt =
                        e.CreatedAtUtc
                })
                .ToListAsync(
                    cancellationToken);

        var totalPages =
            (int)Math.Ceiling(
                totalCount / (double)pageSize);

        return new PagedResponse<MyBookingResponse>
        {
            Items = bookings,

            CurrentPage = page,

            PageSize = pageSize,

            TotalPages = totalPages,

            TotalCount = totalCount,

            HasPrevious = page - 1,

            HasNext = page +1 
        };
    }

    private static string GenerateBookingNumber()
    {
        var timestamp =
            DateTime.UtcNow.ToString("yyyyMMdd");

        var random =
            Guid.NewGuid()
                .ToString("N")[..6]
                .ToUpperInvariant();

        return $"BK-{timestamp}-{random}";
    }
}
