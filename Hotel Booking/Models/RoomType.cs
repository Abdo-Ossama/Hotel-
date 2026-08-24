using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hotel_Booking.Models;

public class RoomType
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Room type name is required.")]
   
    public string Name { get; set; } = string.Empty; // Single, Double, Suite, etc.

    [Required(ErrorMessage = "Description is required.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Base price per night is required.")]
    [Precision(18,2)]
    public decimal BasePricePerNight { get; set; }

    public int MaxAdults { get; set; }


    public int MaxChildren { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}