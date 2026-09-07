using Hotel_Booking.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

public class Booking
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(20, MinimumLength = 8)]
    public string BookingNumber { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;
    public ApplicationUser Customer { get; set; } = null!;

    [Required]
    public DateTime CheckInDate { get; set; }

    [Required]
    public DateTime CheckOutDate { get; set; }

    [Range(1, 365)]
    public int TotalNights { get; set; }


  
    [Precision(18, 2)]
    public decimal GuestCount { get; set; }

    [Precision(18, 2)]
    public decimal DiscountAmount { get; set; } 

    [Precision(18, 2)]
    public decimal TaxAmount { get; set; }

    [Precision(18, 2)]
    public decimal FeeAmount { get; set; }

    [Precision(18, 2)]
    public decimal TotalPrice { get; set; }

    public string Currency { get; set; } = "EGP";

    public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;

    public DateTime? PaymentDueAtUtc { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; } 
    public DateTime? CancelledAtUtc { get; set; }

    [StringLength(500)]
    public string? CancellationReason { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // ─── Navigation Properties ───
    public ICollection<Guest> Guests { get; set; } = new List<Guest>(); 
    public ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();
    public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public Review? Review { get; set; }
}