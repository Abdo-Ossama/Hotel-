using System.ComponentModel.DataAnnotations;

namespace Hotel_Booking.Models
{
        public class Guest
        {
            public Guid Id { get; set; }
            public Guid BookingId { get; set; }
            public Booking Booking { get; set; } = null!;

            [Required]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            public string LastName { get; set; } = string.Empty;

            public string? Email { get; set; }
            public string? Phone { get; set; }


        

        // في Booking.cs:
        public ICollection<Guest> Guests { get; set; } = new List<Guest>();
    }
}
