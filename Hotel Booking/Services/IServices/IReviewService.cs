
namespace Hotel_Booking.Services;

public interface IReviewService
{
    Task<ReviewResponse> CreateReviewAsync(
        CreateReviewRequest createReviewRequest,
        string customerId,
        CancellationToken cancellationToken = default);

    Task<ReviewResponse> UpdateReviewAsync(
        int reviewId,
        UpdateReviewRequest updateReviewRequest,
        string customerId,
        CancellationToken cancellationToken = default);

    Task DeactivateOwnReviewAsync(
        int reviewId,
        string customerId,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<ReviewResponse>> GetReviewsAsync(
        int page,
        CancellationToken cancellationToken = default);

  
    Task DeactivateReviewAsync(
        int reviewId,
        CancellationToken cancellationToken = default);
}