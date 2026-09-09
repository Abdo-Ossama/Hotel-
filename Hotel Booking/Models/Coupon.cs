using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Hotel_Booking.Models;

public class Coupon
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Coupon code is required.")]
    [StringLength(
        20,
        MinimumLength = 3,
        ErrorMessage = "Coupon code must be between 3 and 20 characters."
    )]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Precision(5, 2)]
    [Range(0.01, 100)]
    public decimal DiscountPercentage { get; set; }

    [Range(1, 50)]
    public int UsageLimit { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Used count cannot be negative.")]
    public int UsedCount { get; set; } = 0;

    [Required]
    public DateTime StartDateUtc { get; set; }

    [Required]
    public DateTime ExpiryDateUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<CouponUsage> Usages { get; set; }
        = new List<CouponUsage>();
}