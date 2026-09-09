using Hotel_Booking.DTOs.Request;
using Hotel_Booking.DTOs.Response;
using Hotel_Booking.Enums;
using Hotel_Booking.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace Hotel_Booking.Services;

public class ReviewService : IReviewService
{


    private const int SqlUniqueConstraintViolation = 2627; //violation for UNIQUE constraint  PRIMARY KEY constraint.
    private const int SqlUniqueIndexViolation = 2601; // violation for duplicate




    private readonly IRepository<Models.Review> _reviewRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(
        IRepository<Models.Review> reviewRepository,
        IRepository<Booking> bookingRepository,
        ILogger<ReviewService> logger)
    {
        _reviewRepository = reviewRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }



    public async Task<ReviewResponse> CreateReviewAsync(
        CreateReviewRequest createReviewRequest,
        string customerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ValidationAppException(
                "Invalid user.",
                new Dictionary<string, string[]> { ["CustomerId"] = ["Customer id is required."] });

        if (createReviewRequest.BookingId == Guid.Empty)
            throw new ValidationAppException(
                "Invalid booking id.",
                new Dictionary<string, string[]> { ["BookingId"] = ["Booking id is required."] });

        ValidateRatingAndComment(createReviewRequest.Rating, createReviewRequest.Comment);

        var booking = await _bookingRepository.GetOneAsync(
            e => e.Id == createReviewRequest.BookingId,
            includes: [e => e.Review!],
            tracked: false,
            cancellationToken: cancellationToken);

        if (booking is null || booking.CustomerId != customerId)
        {

            throw new NotFoundException("Booking not found.");
        }

 
        if (booking.Status != BookingStatus.CheckedOut)
            throw new ValidationAppException(
                "Booking is not eligible for review.",
                new Dictionary<string, string[]>
                {
                    ["Booking"] = ["You can only review a booking after your stay has been completed (checked out)."]
                });

     
        if (booking.Review is not null)
            throw new ConflictException("This booking has already been reviewed.");

        var review = new Models.Review
        {
            BookingId = booking.Id,
            CustomerId = customerId,
            Rating = createReviewRequest.Rating,
            Comment = createReviewRequest.Comment.Trim(),
            IsApproved = true, 
            CreatedAtUtc = DateTime.UtcNow
        };

        try
        {
            await _reviewRepository.CreateAysnc(review, cancellationToken);
            await _reviewRepository.CommitAsync(cancellationToken);
        }
      
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {

            _logger.LogWarning(ex,
                "Duplicate review attempt detected for BookingId: {BookingId}.",
                booking.Id);
            throw new ConflictException("This booking has already been reviewed.");
        }

        _logger.LogInformation(
            "Review created. ReviewId: {ReviewId}, BookingId: {BookingId}, CustomerId: {CustomerId}",
            review.Id, booking.Id, customerId);

        return await MapToResponseAsync(review.Id, cancellationToken);
    }

    public async Task<ReviewResponse> UpdateReviewAsync(
    int reviewId,
    UpdateReviewRequest updateReviewRequest,
    string customerId,
    CancellationToken cancellationToken = default)
    {
        if (reviewId <= 0)
            throw new ValidationAppException(
                "Invalid Id.",
                new Dictionary<string, string[]>
                {
                    ["Id"] = ["Review ID must be greater than zero."]
                });

        if (updateReviewRequest.Rating is null &&
            string.IsNullOrWhiteSpace(updateReviewRequest.Comment))
        {
            throw new ValidationAppException(
                "Invalid update.",
                new Dictionary<string, string[]>
                {
                    ["Review"] = ["At least rating or comment must be provided."]
                });
        }

        if (updateReviewRequest.Rating.HasValue &&
            updateReviewRequest.Rating.Value is < 1 or > 5)
        {
            throw new ValidationAppException(
                "Invalid rating.",
                new Dictionary<string, string[]>
                {
                    ["Rating"] = ["Rating must be between 1 and 5 stars."]
                });
        }

        if (updateReviewRequest.Comment is not null)
        {
            var comment = updateReviewRequest.Comment.Trim();

            if (comment.Length < 10)
                throw new ValidationAppException(
                    "Invalid comment.",
                    new Dictionary<string, string[]>
                    {
                        ["Comment"] = ["Comment must be at least 10 characters."]
                    });

            if (comment.Length > 1000)
                throw new ValidationAppException(
                    "Invalid comment.",
                    new Dictionary<string, string[]>
                    {
                        ["Comment"] = ["Comment cannot exceed 1000 characters."]
                    });
        }

        var review = await _reviewRepository.GetOneAsync(
            e => e.Id == reviewId,
            tracked: true,
            cancellationToken: cancellationToken);

        if (review is null || review.CustomerId != customerId)
            throw new NotFoundException("Review not found.");

        if (!review.IsApproved)
            throw new ValidationAppException(
                "Review cannot be updated.",
                new Dictionary<string, string[]>
                {
                    ["Review"] = ["This review is no longer active."]
                });

        review.Rating = updateReviewRequest.Rating ?? review.Rating;

        review.Comment = updateReviewRequest.Comment?.Trim() ?? review.Comment;

        await _reviewRepository.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Review updated. ReviewId: {ReviewId}, CustomerId: {CustomerId}",
            review.Id,
            customerId);

        return await MapToResponseAsync(
            review.Id,
            cancellationToken);
    }
    public async Task DeactivateOwnReviewAsync(
        int reviewId,
        string customerId,
        CancellationToken cancellationToken = default)
    {
        if (reviewId <= 0)
            throw new ValidationAppException(
                "Invalid Id.",
                new Dictionary<string, string[]> { ["Id"] = ["Review ID must be greater than zero."] });

        var review = await _reviewRepository.GetOneAsync(
            e => e.Id == reviewId,
            tracked: true,
            cancellationToken: cancellationToken);

        if (review is null || review.CustomerId != customerId)
            throw new NotFoundException("Review not found.");

        if (!review.IsApproved)
        {
            _logger.LogInformation(
                "Review {ReviewId} already inactive. Skipping.", reviewId);
            return;
        }

        review.IsApproved = false;

        await _reviewRepository.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Review deactivated by owner. ReviewId: {ReviewId}, CustomerId: {CustomerId}",
            reviewId, customerId);
    }


    public async Task DeactivateReviewAsync(
        int reviewId,
        CancellationToken cancellationToken = default)
    {
        if (reviewId <= 0)
            throw new ValidationAppException(
                "Invalid Id.",
                new Dictionary<string, string[]> { ["Id"] = ["Review ID must be greater than zero."] });

        var review = await _reviewRepository.GetOneAsync(
            e => e.Id == reviewId,
            tracked: true,
            cancellationToken: cancellationToken);

        if (review is null)
            throw new NotFoundException("Review not found.");

        if (!review.IsApproved)
        {
            _logger.LogInformation(
                "Review {ReviewId} already inactive. Skipping.", reviewId);
            return;
        }

        review.IsApproved = false;

        await _reviewRepository.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Review deactivated by admin. ReviewId: {ReviewId}", reviewId);
    }


    public async Task<PagedResponse<ReviewResponse>> GetReviewsAsync(
        int page,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 10;

        if (page <= 0)
            throw new ValidationAppException(
                "Invalid page number.",
                new Dictionary<string, string[]> { ["Page"] = ["Page number must be greater than zero."] });

        var reviewQuery = _reviewRepository.GetQueryable(
            e => e.IsApproved,
            includes: [e => e.Customer, e => e.Booking],
            tracked: false);

        var totalCount = await reviewQuery.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var reviews = await reviewQuery
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ReviewResponse
            {
                Id = e.Id,
                BookingId = e.BookingId,
                BookingNumber = e.Booking.BookingNumber,
                CustomerId = e.CustomerId,
                CustomerName = e.Customer.FirstName + " " + e.Customer.LastName,
                Rating = e.Rating,
                Comment = e.Comment,
                CreatedAtUtc = e.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<ReviewResponse>
        {
            Items = reviews,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPrevious = page - 1,
            HasNext = page + 1
        };
    }



    private static void ValidateRatingAndComment(int rating, string comment)
    {
        if (rating is < 1 or > 5)
            throw new ValidationAppException(
                "Invalid rating.",
                new Dictionary<string, string[]> { ["Rating"] = ["Rating must be between 1 and 5 stars."] });

        if (string.IsNullOrWhiteSpace(comment))
            throw new ValidationAppException(
                "Invalid comment.",
                new Dictionary<string, string[]> { ["Comment"] = ["Comment is required."] });

        var trimmedLength = comment.Trim().Length;

        if (trimmedLength < 10)
            throw new ValidationAppException(
                "Invalid comment.",
                new Dictionary<string, string[]>
                {
                    ["Comment"] = [$"Comment must be at least 10 characters."]
                });

        if (trimmedLength > 1000)
            throw new ValidationAppException(
                "Invalid comment.",
                new Dictionary<string, string[]>
                {
                    ["Comment"] = [$"Comment cannot exceed 1000 characters."]
                });
    }

    private async Task<ReviewResponse> MapToResponseAsync(
        int reviewId,
        CancellationToken cancellationToken)
    {
        var review = await _reviewRepository.GetOneAsync(
            e => e.Id == reviewId,
            includes: [e => e.Customer, e => e.Booking],
            tracked: false,
            cancellationToken: cancellationToken);

        if (review is null)
            throw new NotFoundException("Review not found.");

        return new ReviewResponse
        {
            Id = review.Id,
            BookingId = review.BookingId,
            BookingNumber = review.Booking.BookingNumber,
            CustomerId = review.CustomerId,
            CustomerName = $"{review.Customer.FirstName} {review.Customer.LastName}",
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAtUtc = review.CreatedAtUtc
        };
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sqlEx &&
               (sqlEx.Number == SqlUniqueConstraintViolation ||
                sqlEx.Number == SqlUniqueIndexViolation);
    }
}