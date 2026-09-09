using Hotel_Booking.Enums;
using Microsoft.EntityFrameworkCore;
using Coupon = Hotel_Booking.Models.Coupon;

namespace Hotel_Booking.Services;

public class CouponService : ICouponService
{
    private readonly IRepository<Coupon> _couponRepository;
    private readonly IRepository<CouponUsage> _couponUsageRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CouponService(
        IRepository<Coupon> couponRepository,
        IRepository<CouponUsage> couponUsageRepository,
        IRepository<Booking> bookingRepository,
        IUnitOfWork unitOfWork)
    {
        _couponRepository = couponRepository;
        _couponUsageRepository = couponUsageRepository;
        _bookingRepository = bookingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Coupon> CreateAsync(
        CreateCouponRequest  createCouponRequest,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(createCouponRequest.Code))
            throw new ValidationAppException(
                "Invalid coupon code.",
                new Dictionary<string, string[]>
                {
                    ["Code"] = ["Coupon code is required."]
                });

        var code = createCouponRequest.Code
            .Trim()
            .ToUpperInvariant();

        if (createCouponRequest.DiscountPercentage <= 0 ||
            createCouponRequest.DiscountPercentage > 100)
            throw new ValidationAppException(
                "Invalid discount percentage.",
                new Dictionary<string, string[]>
                {
                    ["DiscountPercentage"] =
                    ["Discount percentage must be between 0.01 and 100."]
                });

        if (createCouponRequest.UsageLimit <= 0)
            throw new ValidationAppException(
                "Invalid usage limit.",
                new Dictionary<string, string[]>
                {
                    ["UsageLimit"] =
                    ["Usage limit must be greater than zero."]
                });

        if (createCouponRequest.StartDateUtc >= createCouponRequest.ExpiryDateUtc)
            throw new ValidationAppException(
                "Invalid coupon dates.",
                new Dictionary<string, string[]>
                {
                    ["ExpiryDateUtc"] =
                    ["Expiry date must be after start date."]
                });

        var exists = await _couponRepository
            .AnyAsync(
                e => e.Code == code,
                cancellationToken);

        if (exists)
            throw new ConflictException(
                "A coupon with this code already exists.");

        var coupon = new Coupon
        {
            Code = code,
            DiscountPercentage =
                Math.Round(createCouponRequest.DiscountPercentage, 2), // 2 number after <  .   >
            UsageLimit = createCouponRequest.UsageLimit,
            UsedCount = 0,
            StartDateUtc = createCouponRequest.StartDateUtc,
            ExpiryDateUtc = createCouponRequest.ExpiryDateUtc,
            IsActive = true
        };

        await _couponRepository.CreateAysnc(
            coupon,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return coupon;
    }

    public async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ValidationAppException(
                "Invalid coupon id.",
                new Dictionary<string, string[]>
                {
                    ["Id"] =
                    ["Coupon id must be greater than zero."]
                });

        var coupon = await _couponRepository
            .GetQueryable(
                e => e.Id == id,
                tracked: true)
            .FirstOrDefaultAsync(cancellationToken);

        if (coupon is null)
            throw new NotFoundException(
                "Coupon not found.");

        if (!coupon.IsActive)
            throw new ConflictException(
                "Coupon is already inactive.");

        coupon.IsActive = false;

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<ApplyCouponResponse> ApplyCouponAsync(
        string userId,
        ApplyCouponRequest  applyCouponRequest,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException(
                "User not authenticated.");

        if (applyCouponRequest.BookingId == Guid.Empty)
            throw new ValidationAppException(
                "Invalid booking.",
                new Dictionary<string, string[]>
                {
                    ["BookingId"] =
                    ["Booking id is required."]
                });

        if (string.IsNullOrWhiteSpace(applyCouponRequest.Code))
            throw new ValidationAppException(
                "Invalid coupon code.",
                new Dictionary<string, string[]>
                {
                    ["Code"] =
                    ["Coupon code is required."]
                });

        var code = applyCouponRequest.Code
            .Trim()
            .ToUpperInvariant();

        var booking = await _bookingRepository
            .GetQueryable(
                e => e.Id == applyCouponRequest.BookingId &&
                     e.CustomerId == userId,
                tracked: true)
            .FirstOrDefaultAsync(cancellationToken);

        if (booking is null)
            throw new NotFoundException(
                "Booking not found.");

        if (booking.Status != BookingStatus.PendingPayment)
            throw new ValidationAppException(
                "Coupon cannot be applied to the booking in its current status.",
                new Dictionary<string, string[]>
                {
                    ["Status"] =
                    [$"Current booking status: {booking.Status}."]
                });

        if (booking.AppliedCouponId.HasValue)
            throw new ConflictException(
                "A coupon is already applied to this booking. Remove it first.");

        var coupon = await _couponRepository
            .GetQueryable(
                e => e.Code == code,
                tracked: false)
            .FirstOrDefaultAsync(cancellationToken);

        if (coupon is null)
            throw new NotFoundException(
                "Coupon not found.");

        var now = DateTime.UtcNow;

        if (!coupon.IsActive)
            throw new ValidationAppException(
                "Coupon is inactive.",
                new Dictionary<string, string[]>
                {
                    ["Coupon"] =
                    ["Coupon is inactive."]
                });

        if (now < coupon.StartDateUtc)
            throw new ValidationAppException(
                "Coupon is not active yet.",
                new Dictionary<string, string[]>
                {
                    ["Coupon"] =
                    ["Coupon is not active yet."]
                });

        if (now > coupon.ExpiryDateUtc)
            throw new ValidationAppException(
                "Coupon has expired.",
                new Dictionary<string, string[]>
                {
                    ["Coupon"] =
                    ["Coupon has expired."]
                });

        if (coupon.UsedCount >= coupon.UsageLimit)
            throw new ConflictException(
                "Coupon usage limit has been reached.");

        var alreadyUsed = await _couponUsageRepository
            .AnyAsync(
                e => e.CouponId == coupon.Id &&
                     e.CustomerId == userId,
                cancellationToken);

        if (alreadyUsed)
            throw new ConflictException(
                "You have already used this coupon.");

        var totalBeforeDiscount = booking.SubTotal;

        var discountAmount = Math.Round(
            totalBeforeDiscount *
            coupon.DiscountPercentage /
            100m,
            2);

        var totalAfterDiscount =
            booking.SubTotal
            - discountAmount
            + booking.TaxAmount
            + booking.FeeAmount;

        booking.AppliedCouponId = coupon.Id;
        booking.AppliedCouponCode = coupon.Code;
        booking.AppliedCouponPercentage =
            coupon.DiscountPercentage;

        booking.DiscountAmount = discountAmount;
        booking.TotalPrice = totalAfterDiscount;
        booking.UpdatedAtUtc = now;

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new ApplyCouponResponse
        {
            BookingId = booking.Id,
            CouponCode = coupon.Code,
            DiscountPercentage =
                coupon.DiscountPercentage,
            DiscountAmount = discountAmount,
            TotalBeforeDiscount =
                totalBeforeDiscount,
            TotalAfterDiscount =
                totalAfterDiscount
        };
    }

    public async Task RemoveCouponAsync(
        Guid bookingId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException(
                "User not authenticated.");

        var booking = await _bookingRepository
            .GetQueryable(
                e => e.Id == bookingId &&
                     e.CustomerId == userId,
                tracked: true)
            .FirstOrDefaultAsync(cancellationToken);

        if (booking is null)
            throw new NotFoundException(
                "Booking not found.");

        if (booking.Status != BookingStatus.PendingPayment)
            throw new ValidationAppException(
                "Coupon cannot be removed from the booking in its current status.",
                new Dictionary<string, string[]>
                {
                    ["Status"] =
                    [$"Current booking status: {booking.Status}."]
                });

        if (!booking.AppliedCouponId.HasValue)
            throw new ValidationAppException(
                "No coupon is applied to this booking.",
                new Dictionary<string, string[]>
                {
                    ["Coupon"] =
                    ["No coupon is applied to this booking."]
                });

        booking.AppliedCouponId = null;
        booking.AppliedCouponCode = null;
        booking.AppliedCouponPercentage = 0;
        booking.DiscountAmount = 0;

        booking.TotalPrice =
            booking.SubTotal
            + booking.TaxAmount
            + booking.FeeAmount;

        booking.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    public async Task ConsumeCouponAsync(
        Booking booking,
        CancellationToken cancellationToken = default)
    {
        if (!booking.AppliedCouponId.HasValue)
            return;

        var couponId = booking.AppliedCouponId.Value;

        var coupon = await _couponRepository
            .GetQueryable(
                e => e.Id == couponId,
                tracked: false)
            .FirstOrDefaultAsync(cancellationToken);

        if (coupon is null)
            throw new ValidationAppException(
                "Applied coupon was not found.",
                new Dictionary<string, string[]>
                {
                    ["CouponId"] =
                    ["Applied coupon was not found."]
                });

        var existingUsage = await _couponUsageRepository
            .GetQueryable(
                e => e.BookingId == booking.Id,
                tracked: false)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingUsage is not null)
            return;

        var now = DateTime.UtcNow;

        if (now < coupon.StartDateUtc)
            throw new ConflictException(
                "Coupon is not active yet.");

        if (now > coupon.ExpiryDateUtc)
            throw new ConflictException(
                "Coupon has expired.");

        var affectedRows =
            await _couponRepository
                .GetQueryable(e =>
                    e.Id == couponId &&
                    e.IsActive &&
                    e.UsedCount < e.UsageLimit)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            e => e.UsedCount,
                            e => e.UsedCount + 1),
                    cancellationToken);

        if (affectedRows == 0)
            throw new ConflictException(
                "Coupon usage limit has been reached.");

        var usage = new CouponUsage
        {
            CouponId = couponId,
            CustomerId = booking.CustomerId,
            BookingId = booking.Id,
            UsedAtUtc = now
        };

        await _couponUsageRepository.CreateAysnc(
            usage,
            cancellationToken);
    }
}