namespace Hotel_Booking.Models;

public class RoomAmenity
{
    // composite key 
    public int RoomId { get; set; }

    public Room Room { get; set; } = null!;

    public int AmenityId { get; set; }

    public Amenity Amenity { get; set; } = null!;
}