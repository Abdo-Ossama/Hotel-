

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Hotel_Booking.Models;

public class BookingRoom
{
    public Guid BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    public int RoomId { get; set; }

    public Room Room { get; set; } = null!;

    // Historical snapshot
    [Precision(18, 2)]
    [Required]
    public decimal PricePerNightSnapshot { get; set; }
    [Required]
    public string RoomNumberSnapshot { get; set; } = string.Empty;
    [Required]
    public string RoomTypeNameSnapshot { get; set; } = string.Empty;
}