namespace Hotel_Booking.Services.IServices
{
    public interface IDashboardService
    {
        Task<DashboardResponse> GetDashboardSummaryAsync(
    CancellationToken cancellationToken = default);
    }
}
