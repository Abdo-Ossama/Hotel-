using Hotel_Booking.Enums;
using Hotel_Booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using System.Data;
using System.Security.Claims;

namespace Hotel_Booking.Controllers
{
    [Area(SD.GUEST_AREA)]
    [Route("api/[area]/[controller]")]
    [ApiController]
    public class AdminBookingsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<Room> _roomRepository;
        private readonly IRepository<Booking> _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly PaymobSettings _stripeSettings;
        private readonly ILogger<AdminBookingsController> _logger;


        public AdminBookingsController(
            UserManager<ApplicationUser> userManager,
            IRepository<Room> roomRepository,
            IRepository<Booking> bookingRepository,
            IUnitOfWork unitOfWork,
            IRepository<Payment> paymentRepository,

            IOptions<PaymobSettings> stripeSettings,
            ILogger<AdminBookingsController> logger)
        {
            _userManager = userManager;
            _roomRepository = roomRepository;
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _paymentRepository = paymentRepository;

            _stripeSettings = stripeSettings.Value;
            _logger = logger;

            //StripeConfiguration.ApiKey = _stripeSettings.SecurityKey;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateBookingRequest createBookingRequest,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException();

            if (createBookingRequest.RoomIds == null ||
                !createBookingRequest.RoomIds.Any() ||
                createBookingRequest.RoomIds.Any(id => id <= 0))
            {
                throw new ValidationAppException("Invalid Room IDs.",
                    new Dictionary<string, string[]>
                    {
                        ["RoomIds"] = ["At least one room must be selected, and all Room IDs must be greater than zero."]
                    });
            }

            if (createBookingRequest.CheckIn.Date < DateTime.UtcNow.Date)
                throw new ValidationAppException("Invalid check-in date.",
                    new Dictionary<string, string[]> { ["CheckIn"] = ["Check-in date cannot be in the past."] });

            if (createBookingRequest.CheckOut.Date <= createBookingRequest.CheckIn.Date)
                throw new ValidationAppException("Invalid check-out date.",
                    new Dictionary<string, string[]>
                    {
                        ["CheckOut"] = ["Check-out date must be after check-in date."]
                    });

            var totalNights = (createBookingRequest.CheckOut.Date - createBookingRequest.CheckIn.Date).Days;

            await using var transaction = await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);

            try
            {
                var roomIds = createBookingRequest.RoomIds;

                var rooms = (await _roomRepository.GetAsync(
                 e => roomIds.Contains(e.Id),
                    includes: [e => e.RoomType],
                    tracked: true,
                    cancellationToken: cancellationToken)).ToList();

                if (!rooms.Any())
                    throw new NotFoundException("Room is not found.");

                if (rooms.Count != roomIds.Count)
                {
                    var missingIds = roomIds.Except(rooms.Select(r => r.Id));
                    throw new NotFoundException(
                        $"The following room(s) were not found: {string.Join(", ", missingIds)}.");
                }

               
                var totalCapacity = rooms.Sum(e => e.RoomType.Capacity);

                if (createBookingRequest.GuestCount <= 0 || createBookingRequest.GuestCount > totalCapacity)
                {
                    throw new ValidationAppException("Invalid Guest Count.",
                        new Dictionary<string, string[]>
                        {
                            ["GuestCount"] = [$"Guest count must be between 1 and {totalCapacity}."]
                        });
                }

               
                var hasOverlap = await _bookingRepository.AnyAsync(
                    e => e.BookingRooms.Any(br => roomIds.Contains(br.RoomId)) &&
                         e.Status != BookingStatus.Cancelled &&
                         e.Status != BookingStatus.Expired &&
                         createBookingRequest.CheckIn.Date < e.CheckOutDate &&
                         createBookingRequest.CheckOut.Date > e.CheckInDate,
                    cancellationToken: cancellationToken);

                if (hasOverlap)
                    throw new ConflictException("One or more selected rooms are not available for the selected dates.");

             
                var basePrice = rooms.Sum(r => r.RoomType.BasePricePerNight) * totalNights;
                var taxes = basePrice * 0.14m;
                var total = basePrice + taxes;

                var booking = new Booking
                {
                    BookingNumber = GenerateBookingNumber(),
                    CustomerId = userId,

                    CheckInDate = createBookingRequest.CheckIn.Date,
                    CheckOutDate = createBookingRequest.CheckOut.Date,

                    TotalNights = totalNights,
                    GuestCount = createBookingRequest.GuestCount,

                    TaxAmount = taxes,
                    TotalPrice = total,

                    Status = BookingStatus.PendingPayment,
                    PaymentDueAtUtc = DateTime.UtcNow.AddMinutes(30),
                    CreatedAtUtc = DateTime.UtcNow,

                    BookingRooms = rooms.Select(r => new BookingRoom
                    {
                        RoomId = r.Id,
                        PricePerNightSnapshot = r.RoomType.BasePricePerNight,
                        RoomNumberSnapshot = r.RoomNumber,
                        RoomTypeNameSnapshot = r.RoomType.Name
                    }).ToList(),

                    Guests = createBookingRequest.Guests?
                        .Select(e => new Guest
                        {
                            FirstName = e.FirstName,
                            LastName = e.LastName,
                            Email = e.Email,
                            Phone = e.Phone
                        })
                        .ToList()
                        ?? new List<Guest>()
                };

                await _bookingRepository.CreateAysnc(booking, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return Ok(new APIResponse
                {
                    StatusCode = StatusCodes.Status201Created,
                    Message = ["Booking created successfully."],
                    Data = new BookingResponse
                    {
                        Id = booking.Id,
                        BookingNumber = booking.BookingNumber,
                        RoomIds = booking.BookingRooms.Select(br => br.RoomId).ToList(),
                        CheckIn = booking.CheckInDate,
                        CheckOut = booking.CheckOutDate,
                        GuestCount = (int)booking.GuestCount,
                        TotalNights = booking.TotalNights,
                        TotalPrice = booking.TotalPrice,
                        Status = booking.Status.ToString(),
                        PaymentDueAtUtc = (DateTime)booking.PaymentDueAtUtc
                    }
                });
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static string GenerateBookingNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = Guid.NewGuid().ToString("N")[..6].ToUpper();
            return $"BK-{timestamp}-{random}";
        }

        [HttpGet("my-bookings")]
        public async Task<IActionResult> GetMyBookings(
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User not authenticated.");

            if (page <= 0) page = 1;
            const int pageSize = 5;

            var bookingQuery = _bookingRepository.GetQueryable(
                e => e.CustomerId == userId,
                includes: [e => e.BookingRooms, e => e.Payments],
                tracked: false);

            var totalCount = await bookingQuery.CountAsync(cancellationToken);

            var bookings = await bookingQuery
                .OrderByDescending(e => e.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new MyBookingResponse
                {
                    Id = e.Id,
                    BookingNumber = e.BookingNumber,
                    CheckIn = e.CheckInDate,
                    CheckOut = e.CheckOutDate,
                    TotalNights = e.TotalNights,
                    TotalPrice = e.TotalPrice,
                    Status = e.Status.ToString(),
                    PaymentStatus = e.Payments
                        .OrderByDescending(p => p.CreatedAtUtc)
                        .Select(p => p.Status.ToString())
                        .FirstOrDefault() ?? "No Payment",
                    RoomTypeName = e.BookingRooms
                        .Select(br => br.RoomTypeNameSnapshot)
                        .FirstOrDefault() ?? "N/A",
                    CreatedAt = e.CreatedAtUtc
                })
                .ToListAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return Ok(new APIResponse
            {
                StatusCode = StatusCodes.Status200OK,
                Message = ["Bookings retrieved successfully."],
                Data = new PagedResponse<MyBookingResponse>
                {
                    Items = bookings,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount,
                    HasPrevious = page - 1,
                    HasNext = page + 1
                }
            });
        }

        [HttpGet("{bookingNumber}")]
        public async Task<IActionResult> GetBookingByNumber(
            string bookingNumber,
            CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User not authenticated.");

            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                throw new ValidationAppException(
                    "Invalid booking number.",
                    new Dictionary<string, string[]>
                    {
                        ["BookingNumber"] = ["Booking number is required."]
                    });
            }

            bookingNumber = bookingNumber.Trim();

            var bookingQuery = _bookingRepository.GetQueryable(
                e =>
                    e.BookingNumber == bookingNumber &&
                    e.CustomerId == userId,

                includes:
                [
                    e => e.BookingRooms,
                    e => e.Payments,
                    e => e.Guests
                ],

                tracked: false
            );

            var booking = await bookingQuery
                .Select(e => new BookingDetailsResponse
                {
                    Id = e.Id,

                    BookingNumber = e.BookingNumber,

                    CheckIn = e.CheckInDate,

                    CheckOut = e.CheckOutDate,

                    TotalNights = e.TotalNights,

                    GuestCount = (int)e.GuestCount,

                    TaxAmount = e.TaxAmount,

                    TotalPrice = e.TotalPrice,

                    Status = e.Status.ToString(),

                    PaymentStatus = e.Payments
                        .OrderByDescending(p => p.CreatedAtUtc)
                        .Select(p => p.Status.ToString())
                        .FirstOrDefault() ?? "No Payment",

                    PaymentDueAtUtc = (DateTime)e.PaymentDueAtUtc!,

                    CreatedAt = e.CreatedAtUtc,

                    //  Rooms
                    Rooms = e.BookingRooms
                        .Select(br => new BookingRoomResponse
                        {
                            RoomId = br.RoomId,

                            RoomNumber = br.RoomNumberSnapshot,

                            RoomTypeName = br.RoomTypeNameSnapshot,

                            PricePerNight = br.PricePerNightSnapshot
                        })
                        .ToList(),

                    // ─── Guests ───
                    Guests = e.Guests
                        .Select(g => new BookingGuestResponse
                        {
                            FirstName = g.FirstName,

                            LastName = g.LastName,

                            Email = g.Email!,

                            Phone = g.Phone!
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (booking is null)
                throw new NotFoundException(
                    "Booking not found."
                );

            return Ok(new APIResponse
            {
                StatusCode = StatusCodes.Status200OK,

                Message = ["Booking retrieved successfully."],

                Data = booking
            });
        }


    //    [HttpPost("pay")]
    //    public async Task<IActionResult> Pay(
    //[FromBody] CreatePaymentRequest createPaymentRequest)
    //    {
    //        var booking = await _bookingRepository.GetOneAsync(
    //            e => e.Id == createPaymentRequest.BookingId,
    //            tracked: true,
    //            cancellationToken: default);

    //        if (booking is null)
    //        {
    //            throw new NotFoundException(
    //                $"Booking with id {createPaymentRequest.BookingId} was not found.");
    //        }

    //        if (booking.Status == BookingStatus.Confirmed)
    //        {
    //            throw new ConflictException(
    //                $"Booking {booking.BookingNumber} is already confirmed.");
    //        }

    //        _logger.LogInformation(
    //            "Creating payment for booking. BookingId: {BookingId}, BookingNumber: {BookingNumber}, TotalPrice: {TotalPrice} {Currency}",
    //            booking.Id,
    //            booking.BookingNumber,
    //            booking.TotalPrice,
    //            booking.Currency);

    //        var payment = new Payment
    //        {
    //            Id = Guid.NewGuid(),
    //            BookingId = booking.Id,
    //            Amount = booking.TotalPrice,
    //            Currency = booking.Currency,
    //            Provider = "Stripe",
    //            Status = PaymentStatus.Pending,
    //            CreatedAtUtc = DateTime.UtcNow
    //        };

    //        await _paymentRepository.CreateAysnc(payment);

    //        try
    //        {
    //            var domain = "https://localhost:7190"; 

             
    //            var options = new SessionCreateOptions
    //            {
    //                PaymentMethodTypes = new List<string>
    //                {
    //                    "card"
    //                },

    //                Mode = "payment",
    //                // ... باقي الإعدادات
    //                SuccessUrl = $"{domain}/api/Guest/Checkouts/{booking.BookingNumber}/Success",
    //                CancelUrl = $"{domain}/api/Guest/Checkouts/{booking.BookingNumber}/Cancel",

    //                LineItems = new List<SessionLineItemOptions>
    //                {
    //                    new SessionLineItemOptions
    //                    {
    //                        Quantity = 1,

    //                        PriceData = new SessionLineItemPriceDataOptions
    //                        {
    //                            Currency = booking.Currency.ToLower(),

    //                            UnitAmount =
    //                                (long)(booking.TotalPrice * 100),

    //                            ProductData =
    //                                new SessionLineItemPriceDataProductDataOptions
    //                                {
    //                                    Name =
    //                                        $"Hotel Booking #{booking.BookingNumber}"
    //                                }
    //                        }
    //                    }
    //                },

    //                Metadata = new Dictionary<string, string>
    //                {
    //                    {
    //                        "PaymentId",
    //                        payment.Id.ToString()
    //                    },
    //                    {
    //                        "BookingId",
    //                        booking.Id.ToString()
    //                    },
    //                    {
    //                        "BookingNumber",
    //                        booking.BookingNumber
    //                    }
    //                }
    //            };

    //            var service = new SessionService();

    //            var session = await service.CreateAsync(options);

    //            payment.StripeSessionId = session.Id;
    //            payment.CheckoutUrl = session.Url;

    //            var transaction = new PaymentTransaction
    //            {
    //                Id = Guid.NewGuid(),
    //                PaymentId = payment.Id,
    //                ProviderTransactionReference = session.Id,
    //                StripeEventId = null,
    //                EventType = "stripe_checkout_created",
    //                RawPayload = session.ToJson(),
    //                IsSuccess = true,
    //                ProcessedAtUtc = DateTime.UtcNow
    //            };

    //            await _transactionRepository.CreateAysnc(transaction);

    //            await _paymentRepository.CommitAsync();

    //            _logger.LogInformation(
    //                "Stripe Checkout Session created. SessionId: {SessionId}, PaymentId: {PaymentId}, BookingNumber: {BookingNumber}",
    //                session.Id,
    //                payment.Id,
    //                booking.BookingNumber);

    //            return Ok(new PaymentResponse
    //            {
    //                PaymentId = payment.Id,
    //                CheckoutUrl = session.Url,

    //                Booking = new BookingSummaryResponse
    //                {
    //                    BookingId = booking.Id,
    //                    BookingNumber = booking.BookingNumber,
    //                    CheckInDate = booking.CheckInDate,
    //                    CheckOutDate = booking.CheckOutDate,
    //                    TotalNights = booking.TotalNights,
    //                    TotalPrice = booking.TotalPrice,
    //                    Currency = booking.Currency,
    //                    Status = booking.Status
    //                }
    //            });
    //        }
    //        catch (StripeException ex)
    //        {
    //            _logger.LogWarning(
    //                "Stripe rejected payment. BookingId: {BookingId}, PaymentId: {PaymentId}, StripeError: {Message}",
    //                booking.Id,
    //                payment.Id,
    //                ex.Message);

    //            payment.Status = PaymentStatus.Failed;

    //            var failedTransaction = new PaymentTransaction
    //            {
    //                Id = Guid.NewGuid(),
    //                PaymentId = payment.Id,
    //                ProviderTransactionReference = null,
    //                StripeEventId = null,
    //                EventType = "stripe_error",
    //                RawPayload =
    //                    ex.StripeError?.ToJson() ?? ex.Message,
    //                IsSuccess = false,
    //                ProcessedAtUtc = DateTime.UtcNow
    //            };

    //            await _transactionRepository.CreateAysnc(
    //                failedTransaction);

    //            await _paymentRepository.CommitAsync();

    //            throw new BusinessRuleException(
    //                $"Payment could not be processed: {ex.Message}");
    //        }
    //    }
    } 
}
