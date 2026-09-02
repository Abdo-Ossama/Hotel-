namespace Hotel_Booking.Services
{
    using Microsoft.AspNetCore.Identity;

    using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;

    public class JWTHandler : IJWTHandler
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;

        public JWTHandler(IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration;
            _userManager = userManager;
        }

        public async Task<string?> GenerateTokenAsync(string userId, string email)
        {

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return null;

            List<Claim> claims = new List<Claim>();

            claims.Add(new(ClaimTypes.NameIdentifier, userId));
            claims.Add(new(ClaimTypes.Email, user.Email!));
            claims.Add(new(ClaimTypes.Name, user.UserName!));
            claims.Add(new(
          JwtRegisteredClaimNames.Iat,
          DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
      ));

            var userRoles = await _userManager.GetRolesAsync(user);

            foreach (var item in userRoles)
            {
                claims.Add(new(ClaimTypes.Role, item));
            }

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]!));
            SigningCredentials signingCredentials = new(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
              expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
