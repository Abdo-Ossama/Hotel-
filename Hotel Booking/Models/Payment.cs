using Hotel_Booking.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;


namespace Hotel_Booking.Models;

public class Payment
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    [Required]
    [Precision(18, 2)]
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "EGP";

    public PaymentStatus Status { get; set; }
        = PaymentStatus.Pending;

    public PaymentProviderType Provider { get; set; }

    public DateTime CreatedAtUtc { get; set; }
        = DateTime.UtcNow;

    // تسجيل بيانات الدفع سواء نجحت او فشلت ..
    public ICollection<PaymentTransaction> Transactions { get; set; }
        = new List<PaymentTransaction>();

    // تسجيل بيانات استرجاع الفلوس سواء نجحت او فشلت ..
    public ICollection<Refund> Refunds { get; set; }
        = new List<Refund>();
}