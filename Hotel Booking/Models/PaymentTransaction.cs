using System.ComponentModel.DataAnnotations;

namespace Hotel_Booking.Models;

public class PaymentTransaction
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public Payment Payment { get; set; } = null!;

    [Required(ErrorMessage = "Provider transaction reference is required.")]
    public string ProviderTransactionReference { get; set; } // ProviderTransaction Id
        = string.Empty;


    // success - failed
    [Required(ErrorMessage = "Event type is required.")]
    public string EventType { get; set; }
        = string.Empty;

    // البيانات اللي جايه من خدمة الدفع , لو يوجد خطأ ف من خلاله هعرف ,,
    //  اما لو بيانات صحيحة IsSuccess هتبقى true  مش هحتاج اقرأ الداتا 
    [Required(ErrorMessage = "Raw payload is required.")]
    public string RawPayload { get; set; }
        = string.Empty;

    public bool IsSuccess { get; set; }

    public DateTime ProcessedAtUtc { get; set; }
        = DateTime.UtcNow;
}