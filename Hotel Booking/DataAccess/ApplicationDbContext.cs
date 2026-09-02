using Hotel_Booking.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Hotel_Booking.DataAccess
{

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {





        public ApplicationDbContext(
               DbContextOptions<ApplicationDbContext> options)
               : base(options)
        {
        }

        public DbSet<Amenity> Amenities { get; set; }

        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingRoom> BookingRooms { get; set; }
        public DbSet<BookingService> BookingServices { get; set; }



        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CouponUsage> CouponUsages { get; set; }
        public DbSet<HotelService> HotelServices { get; set; }

        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<Refund> Refunds { get; set; }

        public DbSet<Review> Reviews { get; set; }


        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<RoomAmenity> RoomAmenities { get; set; }
        public DbSet<RoomImage> RoomImages { get; set; }

      

  
       



 

  

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Booking>()
                .HasIndex(e => e.BookingNumber)
                .IsUnique();

            builder.Entity<Booking>()
         .HasIndex(b => b.CustomerId);
           

            builder.Entity<BookingRoom>()
                .HasKey(e => new
                {
                    e.RoomId,
                    e.BookingId
                });
            builder.Entity<RoomAmenity>()
                .HasKey(e => new
                {
                    e.RoomId,
                    e.AmenityId
                });
            builder.Entity<BookingService>()
                .HasKey(e => new
                {
                    e.BookingId,
                    e.HotelServiceId
                });

            builder.Entity<Review>()
     .HasOne(e => e.Customer)
     .WithMany(e => e.Reviews)
     .HasForeignKey(e => e.CustomerId)
     .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<Booking>()
     .HasOne(e => e.Customer)
     .WithMany(e => e.Bookings)
     .HasForeignKey(e => e.CustomerId)
     .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<Review>()
     .HasOne(e => e.Booking)
     .WithOne(e => e.Review)
     .HasForeignKey<Review>(e => e.BookingId)
     .OnDelete(DeleteBehavior.Restrict);

            // RowVersion ( ROOM)

            builder.Entity<Room>()
    .Property(r => r.RowVersion)
    .IsRowVersion();
        }
        



        
    }

   
}