using Hotel_Booking.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Hotel_Booking.Models;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    [Required]
    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [Required]
    public string Currency { get; set; } = "EGP";

    [Required]
    public string Provider { get; set; } = "Paymob";

    public string? ProviderOrderId { get; set; }

    public string? ProviderTransactionId { get; set; }

    public string? CheckoutUrl { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string? LastEventPayload { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }

    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}