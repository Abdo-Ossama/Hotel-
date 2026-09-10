using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Hotel_Booking.Enums;
namespace Hotel_Booking.Services;

public class DashboardService : IDashboardService
{
    private const string CacheKey = CacheKeys.DashboardSummary;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IRepository<Refund> _refundRepository;
    private readonly IRepository<Room> _roomRepository;
    private readonly IRepository<Review> _reviewRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICacheService _cacheService;
    private readonly HotelSettings _hotelSettings;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IRepository<Booking> bookingRepository,
        IRepository<Payment> paymentRepository,
        IRepository<Refund> refundRepository,
        IRepository<Room> roomRepository,
        IRepository<Review> reviewRepository,
        UserManager<ApplicationUser> userManager,
        ICacheService cacheService,
        IOptions<HotelSettings> hotelSettings,
        ILogger<DashboardService> logger)
    {
        _bookingRepository = bookingRepository;
        _paymentRepository = paymentRepository;
        _refundRepository = refundRepository;
        _roomRepository = roomRepository;
        _reviewRepository = reviewRepository;
        _userManager = userManager;
        _cacheService = cacheService;
        _hotelSettings = hotelSettings.Value;
        _logger = logger;
    }

    public async Task<DashboardResponse> GetDashboardSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var cached = await _cacheService.GetAsync<DashboardResponse>(CacheKey);

        if (cached is not null)
        {
            _logger.LogInformation("Cache hit for {CacheKey}.", CacheKey);
            return cached;
        }

        _logger.LogInformation("Cache miss for {CacheKey}. Building dashboard summary.", CacheKey);

        var hotelTimeZone = TimeZoneInfo.FindSystemTimeZoneById(_hotelSettings.TimeZoneId);
        var nowUtc = DateTime.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, hotelTimeZone);

        var todayLocalStart = nowLocal.Date;
        var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(todayLocalStart, DateTimeKind.Unspecified), hotelTimeZone);
        var todayEndUtc = todayStartUtc.AddDays(1);
        var yesterdayStartUtc = todayStartUtc.AddDays(-1);

        var daysSinceSaturday = ((int)todayLocalStart.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
        var weekLocalStart = todayLocalStart.AddDays(-daysSinceSaturday);
        var weekStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(weekLocalStart, DateTimeKind.Unspecified), hotelTimeZone);

        var monthLocalStart = new DateTime(todayLocalStart.Year, todayLocalStart.Month, 1);
        var monthStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(monthLocalStart, DateTimeKind.Unspecified), hotelTimeZone);

     
        var revenue = await BuildRevenueSummaryAsync(
            todayStartUtc, todayEndUtc, yesterdayStartUtc, weekStartUtc, monthStartUtc, cancellationToken);

        var bookings = await BuildBookingsSummaryAsync(todayStartUtc, todayEndUtc, cancellationToken);
        var rooms = await BuildRoomsSummaryAsync(cancellationToken);
        var performance = await BuildBookingPerformanceSummaryAsync(cancellationToken);
        var customers = await BuildCustomersSummaryAsync(todayStartUtc, todayEndUtc, monthStartUtc, cancellationToken);
        var reviews = await BuildReviewsSummaryAsync(cancellationToken);

        var pendingPayments = await _paymentRepository.GetQueryable(
        p => p.Status == PaymentStatus.Pending,
        tracked: false)
     .CountAsync(cancellationToken);

        var response = new DashboardResponse
        {
            GeneratedAtUtc = nowUtc,
            Currency = "EGP", 
            Revenue = revenue,
            Bookings = bookings,
            Rooms = rooms,
            BookingPerformance = performance,
            Customers = customers,
            Reviews = reviews,
            NeedsAttention = new NeedsAttentionSummary
            {
                PendingBookings = bookings.Pending,
                PendingPayments = pendingPayments,
                MaintenanceRooms = rooms.Maintenance
            }
        };

        await _cacheService.SetAsync(CacheKey, response, CacheDuration);

        return response;
    }


    private async Task<RevenueSummary> BuildRevenueSummaryAsync(
        DateTime todayStartUtc,
        DateTime todayEndUtc,
        DateTime yesterdayStartUtc,
        DateTime weekStartUtc,
        DateTime monthStartUtc,
        CancellationToken cancellationToken)
    {

        var paidQuery = _paymentRepository.GetQueryable(
            p => p.Status == PaymentStatus.Paid && p.PaidAtUtc != null,
            tracked: false);


        var refundQuery = _refundRepository.GetQueryable(
            r => r.Status == RefundStatus.Refunded,
            tracked: false);

        var today = await paidQuery
            .Where(p => p.PaidAtUtc >= todayStartUtc && p.PaidAtUtc < todayEndUtc)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var yesterday = await paidQuery
            .Where(p => p.PaidAtUtc >= yesterdayStartUtc && p.PaidAtUtc < todayStartUtc)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var thisWeek = await paidQuery
            .Where(p => p.PaidAtUtc >= weekStartUtc)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var thisMonth = await paidQuery
            .Where(p => p.PaidAtUtc >= monthStartUtc)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var todayRefunds = await refundQuery
            .Where(r => r.CreatedAtUtc >= todayStartUtc && r.CreatedAtUtc < todayEndUtc)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

        var weekRefunds = await refundQuery
            .Where(r => r.CreatedAtUtc >= weekStartUtc)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

        var monthRefunds = await refundQuery
            .Where(r => r.CreatedAtUtc >= monthStartUtc)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

        var avgBookingValue = await _bookingRepository.GetQueryable(
                b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Expired,
                tracked: false)
            .Select(b => (decimal?)b.TotalPrice)
            .AverageAsync(cancellationToken) ?? 0m;

        decimal? todayVsYesterdayPercent = yesterday > 0
            ? Math.Round(((today - yesterday) / yesterday) * 100, 2)
            : null;

        return new RevenueSummary
        {
            Today = today,
            ThisWeek = thisWeek,
            ThisMonth = thisMonth,
            TodayRefunds = todayRefunds,
            ThisWeekRefunds = weekRefunds,
            ThisMonthRefunds = monthRefunds,
            TodayNetRevenue = today - todayRefunds,
            ThisWeekNetRevenue = thisWeek - weekRefunds,
            ThisMonthNetRevenue = thisMonth - monthRefunds,
            AverageBookingValue = Math.Round(avgBookingValue, 2),
            TodayVsYesterdayPercent = todayVsYesterdayPercent
        };
    }


    private async Task<BookingsSummary> BuildBookingsSummaryAsync(
        DateTime todayStartUtc,
        DateTime todayEndUtc,
        CancellationToken cancellationToken)
    {
        var query = _bookingRepository.GetQueryable(tracked: false);

        var statusCounts = await query
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int CountOf(BookingStatus status) =>
            statusCounts.FirstOrDefault(x => x.Status == status)?.Count ?? 0;

        var todayCount = await query.CountAsync(
            b => b.CreatedAtUtc >= todayStartUtc && b.CreatedAtUtc < todayEndUtc,
            cancellationToken);

        return new BookingsSummary
        {
            Today = todayCount,
            Total = statusCounts.Sum(x => x.Count),
            Pending = CountOf(BookingStatus.PendingPayment),
            Confirmed = CountOf(BookingStatus.Confirmed),
            CheckedIn = CountOf(BookingStatus.CheckedIn),
            CheckedOut = CountOf(BookingStatus.CheckedOut),
            Cancelled = CountOf(BookingStatus.Cancelled),
            Expired = CountOf(BookingStatus.Expired),
            NoShow = CountOf(BookingStatus.NoShow)
        };
    }

    private async Task<RoomsSummary> BuildRoomsSummaryAsync(
        CancellationToken cancellationToken)
    {
        var statusCounts = await _roomRepository.GetQueryable(tracked: false)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int CountOf(RoomStatus status) =>
            statusCounts.FirstOrDefault(x => x.Status == status)?.Count ?? 0;

        var total = statusCounts.Sum(x => x.Count);
        var occupied = CountOf(RoomStatus.Occupied);
        var maintenance = CountOf(RoomStatus.Maintenance);
        var outOfService = CountOf(RoomStatus.OutOfService);

        var sellableRooms = total - maintenance - outOfService;

        return new RoomsSummary
        {
            Total = total,
            Available = CountOf(RoomStatus.Available),
            Reserved = CountOf(RoomStatus.Reserved),
            Occupied = occupied,
            Cleaning = CountOf(RoomStatus.Cleaning),
            Maintenance = maintenance,
            OutOfService = outOfService,
            OccupancyRate = sellableRooms > 0
                ? Math.Round(occupied * 100m / sellableRooms, 2)
                : 0m
        };
    }

   

    private async Task<BookingPerformanceSummary> BuildBookingPerformanceSummaryAsync(
        CancellationToken cancellationToken)
    {
    
        var topRoomType = await _bookingRepository.GetQueryable(
                b => b.Status != BookingStatus.Cancelled &&
                     b.Status != BookingStatus.Expired &&
                     b.Status != BookingStatus.NoShow,
                tracked: false)
            .SelectMany(b => b.BookingRooms)
            .GroupBy(br => br.RoomTypeNameSnapshot)
            .Select(g => new { RoomType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefaultAsync(cancellationToken);

        return new BookingPerformanceSummary
        {
            MostBookedRoomType = topRoomType?.RoomType ?? "N/A",
            MostBookedRoomTypeCount = topRoomType?.Count ?? 0
        };
    }

    private async Task<CustomersSummary> BuildCustomersSummaryAsync(
        DateTime todayStartUtc,
        DateTime todayEndUtc,
        DateTime monthStartUtc,
        CancellationToken cancellationToken)
    {
        var guestUsers = await _userManager.GetUsersInRoleAsync(SD.GUEST_ROLE);
        var guestIds = guestUsers.Select(u => u.Id).ToHashSet();

        var total = guestIds.Count;

        var newToday = guestUsers.Count(u =>
            u.CreatedAtUtc >= todayStartUtc && u.CreatedAtUtc < todayEndUtc);

        var newThisMonth = guestUsers.Count(u =>
            u.CreatedAtUtc >= monthStartUtc);

        return new CustomersSummary
        {
            TotalCustomers = total,
            NewCustomersToday = newToday,
            NewCustomersThisMonth = newThisMonth
        };
    }

    private async Task<ReviewsSummary> BuildReviewsSummaryAsync(
        CancellationToken cancellationToken)
    {
        var reviewQuery = _reviewRepository.GetQueryable(_ => true, tracked: false); // -=> true => Discard 

        var total = await reviewQuery.CountAsync(cancellationToken);
        var active = await reviewQuery.Where(r => r.IsApproved).CountAsync(cancellationToken);

        var averageRating = active > 0
            ? await _reviewRepository.GetQueryable(r => r.IsApproved, tracked: false)
                .Select(r => (decimal?)r.Rating)
                .AverageAsync(cancellationToken) ?? 0m
            : 0m;

        return new ReviewsSummary
        {
            TotalReviews = total,
            ActiveReviews = active,
            InactiveReviews = total - active,
            AverageRating = Math.Round(averageRating, 2)
        };
    }
}