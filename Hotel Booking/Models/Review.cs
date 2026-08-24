

using System.ComponentModel.DataAnnotations;

namespace Hotel_Booking.Models;

public class Review
{
    public int Id { get; set; }

    public Guid BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    [Required]
    public string CustomerId { get; set; } = null!;

    public ApplicationUser Customer { get; set; } = null!;
    [Required(ErrorMessage = "Rating score is required.")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
    public int Rating { get; set; }
    [Required]
    public string Comment { get; set; } = string.Empty;

    public bool IsApproved { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
        = DateTime.UtcNow;
}