
using Microsoft.EntityFrameworkCore;

namespace Hotel_Booking.Models;

public class BookingExtraService
{
    public Guid BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    public int HotelServiceId { get; set; }

    public HotelService HotelService { get; set; } = null!;

    [Precision(18, 2)]
    public decimal UnitPriceSnapshot { get; set; }

    public int Quantity { get; set; }
}