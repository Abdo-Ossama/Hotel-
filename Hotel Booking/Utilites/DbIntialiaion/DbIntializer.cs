using Hotel_Booking.DataAccess;
using Hotel_Booking.Models;
using Hotel_Booking.Utilites;
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
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context, ILogger<DbInitializer> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task Initialize()
        {
            
            
                if (_context.Database.GetPendingMigrations().Any())
                    _context.Database.Migrate();

                if (!_roleManager.Roles.Any())
                {
                    await _roleManager.CreateAsync(new(SD.GUEST_ROLE));
                    await _roleManager.CreateAsync(new(SD.ADMIN_ROLE));
                

                    await _userManager.CreateAsync(new()
                    {
                        Email = "SuperAdmin@hotel.com",
                        EmailConfirmed = true,
                        FirstName = "Super",
                        LastName = "Admin",
                        UserName = "SuperAdmin",
                    }, "Admin123$");

                    var user = await _userManager.FindByEmailAsync("SuperAdmin@hotel.com");

                    await _userManager.AddToRoleAsync(user!, SD.ADMIN_ROLE);
                }
            }
          
        }
    }

