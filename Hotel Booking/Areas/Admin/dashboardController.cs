using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Booking.Areas.Admin;

[Area(SD.ADMIN_AREA)]
[Route("api/[area]/[controller]")]
[ApiController]
[Authorize(Roles = SD.ADMIN_ROLE)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [Authorize(Roles = SD.ADMIN_ROLE)]
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardResponse>> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var summary = await _dashboardService.GetDashboardSummaryAsync(cancellationToken);
        return Ok(summary);
    }
}