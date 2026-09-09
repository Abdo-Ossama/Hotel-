using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Hotel_Booking.Models;

public class HotelService
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is Required ..")]
    public string Name { get; set; } = string.Empty;
    [Precision(18, 2)]
    public decimal Price { get; set; }

    public string Currency { get; set; } = "EGP";

    public bool IsActive { get; set; } = true;

    public ICollection<BookingExtraService> BookingServices { get; set; }
        = new List<BookingExtraService>();
}