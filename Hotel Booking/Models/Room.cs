using Hotel_Booking.Enums;
using Hotel_Booking.Models;
using System.ComponentModel.DataAnnotations;


namespace Hotel_Booking.Models;

public class Room
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Room number is required.")]
    public string RoomNumber { get; set; } = string.Empty;
    [Required]
    public int Floor { get; set; }
    [Required]
    public RoomStatus Status { get; set; }
        = RoomStatus.Available;

    public int RoomTypeId { get; set; }

    public RoomType RoomType { get; set; } = null!;

    // Optimistic Concurrency
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<RoomAmenity> RoomAmenities { get; set; }
        = new List<RoomAmenity>();

    public ICollection<RoomImage> RoomImages { get; set; }
        = new List<RoomImage>();

    public ICollection<BookingRoom> BookingRooms { get; set; }
        = new List<BookingRoom>();
}