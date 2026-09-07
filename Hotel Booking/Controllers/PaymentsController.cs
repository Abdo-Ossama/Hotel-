using Hotel_Booking.DTOs.Request;
using Hotel_Booking.DTOs.Response;
using Hotel_Booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Hotel_Booking.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IHotelPaymentService _hotelPaymentService;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly ILogger<PaymentsController> _logger;
    private readonly IOptions<PaymobSettings> _paymobSettings;

    public PaymentsController(
        IHotelPaymentService hotelPaymentService,
        IRepository<Payment> paymentRepository,
        ILogger<PaymentsController> logger,
        IOptions<PaymobSettings> paymobSettings)
    {
        _hotelPaymentService = hotelPaymentService;
        _paymentRepository = paymentRepository;
        _logger = logger;
        _paymobSettings = paymobSettings;
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CreatePaymentResponse>> CreateCheckout(
        [FromBody] CreatePaymentRequest createPaymentRequest,
        CancellationToken cancellationToken)
    {
        if (createPaymentRequest.BookingId == Guid.Empty)
            return BadRequest("BookingId is required.");

        var paymentUrl = await _hotelPaymentService.CreateCheckoutAsync(
            createPaymentRequest.BookingId,
            cancellationToken);

        return Ok(new CreatePaymentResponse
        {
            PaymentUrl = paymentUrl
        });
    }

    [AllowAnonymous]
    [HttpPost("paymob/callback")]
    public async Task<IActionResult> PaymobCallback(
        [FromQuery(Name = "hmac")] string hmac,
        [FromBody] PaymobCallbackPayload payload,
        CancellationToken cancellationToken)
    {
        if (payload?.Obj is null)
        {
            _logger.LogWarning("Paymob callback received with invalid payload.");
            return BadRequest();
        }


        await _hotelPaymentService.HandlePaymentCallbackAsync(payload.Obj, cancellationToken);

        return Ok();
    }
    [HttpGet("paymob/redirect")]
    public IActionResult PaymobRedirect()
    {
       
        return Ok("Payment completed successfully! You can close this window.");
    }
}