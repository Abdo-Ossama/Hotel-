
using Hotel_Booking.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Hotel_Booking.Models;
public class Refund
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public Payment Payment { get; set; } = null!;
    [Required]
    [Precision(18,2)]
    public decimal Amount { get; set; }

    [Required]
    public string Currency { get; set; } = "EGP";

    [Required]
    public string Reason { get; set; } = string.Empty;

    public RefundStatus Status { get; set; }
        = RefundStatus.RefundPending;

    public string? ProviderRefundReference { get; set; }

    public DateTime CreatedAtUtc { get; set; }
        = DateTime.UtcNow;

    public DateTime? ProcessedAtUtc { get; set; }
}