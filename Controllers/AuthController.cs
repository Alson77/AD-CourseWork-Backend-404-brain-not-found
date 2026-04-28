using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        // Hardcoded users for coursework demonstration
        private static readonly List<AppUser> Users = new()
        {
            new AppUser { Name = "Admin User",     Email = "admin@autoparts.com",    Password = "Admin@123",    Role = "Admin" },
            new AppUser { Name = "John Customer",  Email = "customer@autoparts.com", Password = "Customer@123", Role = "Customer" }
        };

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        // POST api/auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Find user by email and password
            var user = Users.FirstOrDefault(u =>
                u.Email.ToLower() == request.Email.ToLower() &&
                u.Password == request.Password);

            if (user == null)
                return Unauthorized(new { message = "Invalid email or password." });

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                Name  = user.Name,
                Email = user.Email,
                Role  = user.Role
            });
        }

        private string GenerateJwtToken(AppUser user)
        {
            var jwtKey = _config["Jwt:Key"] ?? "VehiclePartsSecretKey2026!ForCoursework";
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name,  user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role,  user.Role)
            };

            var token = new JwtSecurityToken(
                issuer:   _config["Jwt:Issuer"]   ?? "VehiclePartsBackend",
                audience: _config["Jwt:Audience"] ?? "VehiclePartsFrontend",
                claims:   claims,
                expires:  DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
