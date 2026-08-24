using Hotel_Booking.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hotel_Booking.Models;

public class Booking
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Booking number is required..")]
    [StringLength(20, MinimumLength = 8, ErrorMessage = "Booking number must be between 8 and 20 characters.")]
    
    public string BookingNumber { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public ApplicationUser Customer { get; set; } = null!;

    [Required(ErrorMessage = "Check-in date is required.")]
    public DateTime CheckInDate { get; set; }

    [Required(ErrorMessage = "Check-out date is required.")]
    public DateTime CheckOutDate { get; set; }

    [Range(1, 365, ErrorMessage = "Total nights must be between 1 and 365.")]
    public int TotalNights { get; set; }

    [Required]
    public int AdultsCount { get; set; }
    [Required]
    public int ChildrenCount { get; set; }

    [Precision(18, 2)]
    [Required]
    public decimal Subtotal { get; set; }

    [Precision(18, 2)]
    [Required]

    public decimal TaxAmount { get; set; }

    [Precision(18, 2)]
    [Required]
    public decimal FeeAmount { get; set; }

    [Precision(18, 2)]
    [Required]
    public decimal TotalPrice { get; set; }

    [Required(ErrorMessage = "Currency code is required.")]
    
    public string Currency { get; set; } = "EGP";

    [Required]
    public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;

    public DateTime? PaymentDueAtUtc { get; set; }

    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CancelledAtUtc { get; set; }

    [StringLength(500, ErrorMessage = "Cancellation reason cannot exceed 500 characters.")]
    public string? CancellationReason { get; set; }

   
    public ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();

    public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public Review? Review { get; set; }
}