using Hotel_Booking.DataAccess;
using Hotel_Booking.Utilites.DbIntialiaion;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hotel_Booking.API.Utility.DbInitializers
{
    public class DbInitializer : IDbIntializer
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public DbInitializer(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }

        public async Task Initialize()
        {
          
            if ((await _context.Database.GetPendingMigrationsAsync()).Any())
            {
                await _context.Database.MigrateAsync();
            }

            // 2. create roles ..
            if (!await _roleManager.RoleExistsAsync(SD.RECEPTIONIST_ROLE))
                await _roleManager.CreateAsync(new IdentityRole(SD.RECEPTIONIST_ROLE));

            if (!await _roleManager.RoleExistsAsync(SD.GUEST_ROLE))
                await _roleManager.CreateAsync(new IdentityRole(SD.GUEST_ROLE));

            if (!await _roleManager.RoleExistsAsync(SD.ADMIN_ROLE))
                await _roleManager.CreateAsync(new IdentityRole(SD.ADMIN_ROLE));

            var adminEmail = "Admin@hotel.com";
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Hotel",
                    LastName = "Admin",
                    UserName = "abdo_Admin"
                };

                var result = await _userManager.CreateAsync(newAdmin, "Admin123$");

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(newAdmin, SD.ADMIN_ROLE);
                }
            }
        }
    }
}