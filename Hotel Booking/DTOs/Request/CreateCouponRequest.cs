using System.ComponentModel.DataAnnotations;

public class CreateCouponRequest
{

    public string Code { get; set; } = string.Empty;

    public decimal DiscountPercentage { get; set; }

    public int UsageLimit { get; set; }

    public DateTime StartDateUtc { get; set; }

    public DateTime ExpiryDateUtc { get; set; }
}