using Hotel_Booking.DataAccess;
using Hotel_Booking.DTOs.Request;
using Hotel_Booking.Enums;
using Hotel_Booking.Exceptions;
using Hotel_Booking.Models;
using Hotel_Booking.Repositories.IRepositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace Hotel_Booking.Services;

public class HotelPaymentService : IHotelPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly PaymobSettings _paymobSettings;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<HotelPaymentService> _logger;

    public HotelPaymentService(
        HttpClient httpClient,
        IOptions<PaymobSettings> paymobSettings,
        IRepository<Booking> bookingRepository,
        IRepository<Payment> paymentRepository,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor,
        ILogger<HotelPaymentService> logger)
    {
        _httpClient = httpClient;
        _paymobSettings = paymobSettings.Value;
        _bookingRepository = bookingRepository;
        _paymentRepository = paymentRepository;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<string> CreateCheckoutAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetOneAsync(
            b => b.Id == bookingId,
            cancellationToken: cancellationToken);

        if (booking is null)
            throw new NotFoundException("Booking not found.");

        var amount = booking.TotalPrice;

        if (amount <= 0)
            throw new InvalidOperationException(
                "Booking amount must be greater than zero.");

        var amountCents = Convert.ToInt32(amount * 100);

        var authToken = await GetAuthenticationTokenAsync(cancellationToken);

        var paymobBooking = await CreatePaymobBookingAsync(
            authToken,
            booking,
            amountCents,
            cancellationToken);

        var paymentKey = await GeneratePaymentKeyAsync(
            authToken,
            booking,
            paymobBooking.PaymobBookingId,
            amountCents,
            cancellationToken);

        var checkoutUrl = BuildCheckoutUrl(paymentKey.Token);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = amount,
            Currency = "EGP",
            Provider = "Paymob",
            ProviderOrderId = paymobBooking.PaymobBookingId.ToString(),
            CheckoutUrl = checkoutUrl,
            Status = PaymentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _paymentRepository.CreateAysnc(payment, cancellationToken);
        await _paymentRepository.CommitAsync(cancellationToken);

        return checkoutUrl;
    }

    public async Task HandlePaymentCallbackAsync(
        PaymobTransactionObj transaction,
        CancellationToken cancellationToken = default)
    {
        if (transaction?.Order is null)
        {
            _logger.LogWarning("Paymob callback received with a null order/transaction.");
            return;
        }

        var payment = await _paymentRepository.GetOneAsync(
            p => p.ProviderOrderId == transaction.Order.Id.ToString(),
            cancellationToken: cancellationToken);

        if (payment is null)
        {
            _logger.LogWarning(
                "Paymob callback received for unknown ProviderOrderId {ProviderOrderId}.",
                transaction.Order.Id);
            return;
        }

        if (payment.Status == PaymentStatus.Paid || payment.Status == PaymentStatus.Failed)
        {
            _logger.LogInformation(
                "Paymob callback for Payment {PaymentId} already processed. Ignoring duplicate.",
                payment.Id);
            return;
        }

        payment.Status = transaction.Success ? PaymentStatus.Paid : PaymentStatus.Failed;
        payment.ProviderTransactionId = transaction.Id.ToString();      
        payment.LastEventPayload = JsonSerializer.Serialize(transaction); 
        payment.UpdatedAtUtc = DateTime.UtcNow;                         

        _paymentRepository.Update(payment);

        if (transaction.Success)
        {
            var booking = await _bookingRepository.GetOneAsync(
                b => b.Id == payment.BookingId,
                cancellationToken: cancellationToken);

            if (booking is not null)
            {
                booking.Status = BookingStatus.Confirmed;
                booking.ConfirmedAtUtc = DateTime.UtcNow;
                booking.UpdatedAtUtc = DateTime.UtcNow;
                _bookingRepository.Update(booking);
            }
            else
            {
                _logger.LogWarning(
                    "Payment {PaymentId} succeeded but Booking {BookingId} was not found.",
                    payment.Id, payment.BookingId);
            }
        }

        await _paymentRepository.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} updated to {Status} via Paymob callback.",
            payment.Id, payment.Status);
    }

    private async Task<string> GetAuthenticationTokenAsync(
        CancellationToken cancellationToken)
    {
        var request = new PaymobAuthRequest
        {
            ApiKey = _paymobSettings.ApiKey
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_paymobSettings.BaseUrl}/auth/tokens",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Paymob Auth Error: {errorContent}");
        }

        var result = await response.Content
            .ReadFromJsonAsync<PaymobAuthResponse>(cancellationToken);

        if (result is null || string.IsNullOrWhiteSpace(result.Token))
            throw new InvalidOperationException(
                "Paymob authentication failed.");

        return result.Token;
    }

    private async Task<PaymobBookingResponse> CreatePaymobBookingAsync(
        string authToken,
        Booking booking,
        int amountCents,
        CancellationToken cancellationToken)
    {
        var request = new PaymobBookingRequest
        {
            AuthToken = authToken,
            DeliveryNeeded = false,
            AmountCents = amountCents,
            Currency = "EGP",
            MerchantBookingId = booking.Id.ToString(),
            Items =
            [
                new PaymobBookingItem
                {
                    Name = $"Hotel Booking #{booking.Id}",
                    Description = "Hotel room booking",
                    Quantity = 1,
                    AmountCents = amountCents
                }
            ]
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_paymobSettings.BaseUrl}/ecommerce/orders",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Paymob Order Error Details: {errorContent}");
        }

        var result = await response.Content
            .ReadFromJsonAsync<PaymobBookingResponse>(cancellationToken);

        if (result is null || result.PaymobBookingId <= 0)
            throw new InvalidOperationException(
                "Paymob booking creation failed.");

        return result;
    }

    private async Task<PaymobPaymentKeyResponse> GeneratePaymentKeyAsync(
        string authToken,
        Booking booking,
        long paymobBookingId,
        int amountCents,
        CancellationToken cancellationToken)
    {
        string userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            throw new NotFoundException("User not found.");

        var request = new PaymobPaymentKeyRequest
        {
            AuthToken = authToken,
            AmountCents = amountCents,
            Expiration = 3600,
            PaymobBookingId = paymobBookingId,
            Currency = "EGP",
            IntegrationId = _paymobSettings.IntegrationId,
            BillingData = new PaymobBillingData
            {
                Email = user.Email!,
                FirstName = string.IsNullOrEmpty(user.FirstName) ? "Test" : user.FirstName,
                LastName = string.IsNullOrEmpty(user.LastName) ? "User" : user.LastName,
                PhoneNumber = string.IsNullOrEmpty(user.PhoneNumber) ? "01234567891" : user.PhoneNumber
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_paymobSettings.BaseUrl}/acceptance/payment_keys",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Paymob Payment Key Error Details: {errorContent}");
        }

        var result = await response.Content
            .ReadFromJsonAsync<PaymobPaymentKeyResponse>(cancellationToken);

        if (result is null || string.IsNullOrWhiteSpace(result.Token))
            throw new InvalidOperationException(
                "Paymob payment key generation failed.");

        return result;
    }

    private string BuildCheckoutUrl(string paymentToken)
    {
        return $"{_paymobSettings.BaseUrl}/acceptance/iframes/" +
               $"{_paymobSettings.IframeId}?payment_token={paymentToken}";
    }
}