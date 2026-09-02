using System.ComponentModel.DataAnnotations;

namespace Hotel_Booking.Models;

public class RoomImage
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Image URL is required.")]
 
    public string Url { get; set; } = string.Empty;

    public bool IsPrimary { get; set; } = false;

    [Required(ErrorMessage = "Room reference is required.")]
    public int RoomId { get; set; }


    public Room Room { get; set; } = null!;
}