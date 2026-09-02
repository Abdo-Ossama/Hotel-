using System.ComponentModel.DataAnnotations;

namespace Hotel_Booking.Models;

public class Amenity
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Amenity name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Amenity name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "Icon path or class name cannot exceed 250 characters.")]
    public string? Icon { get; set; }

    // Navigation Property
    public ICollection<RoomAmenity> RoomAmenities { get; set; }
        = new List<RoomAmenity>();

}