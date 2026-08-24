using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Hotel_Booking.Models;

public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
   
    public string LastName { get; set; } = string.Empty;

    public bool IsBlocked { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Booking> Bookings { get; set; }
        = new List<Booking>();

    public ICollection<CouponUsage> CouponUsages { get; set; }
        = new List<CouponUsage>();
}