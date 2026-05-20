using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace VehiclePartsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public AuthController(IConfiguration config, AppDbContext context)
        {
            _config = config;
            _context = context;
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Find active user by email and password from DB
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email.ToLower() == request.Email.ToLower() &&
                u.Password == request.Password &&
                u.IsActive);

            if (user == null)
                return Unauthorized(new { message = "Invalid email, password, or account inactive." });

            int? customerId = null;
            if (user.Role == "Customer")
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AppUserId == user.Id);
                if (customer != null)
                {
                    customerId = customer.Id;
                }
            }

            // Generate JWT token
            var token = GenerateJwtToken(user, customerId);

            return Ok(new AuthResponse
            {
                Token = token,
                Id = user.Id,
                CustomerId = customerId,
                Name  = user.Name,
                Email = user.Email,
                Role  = user.Role
            });
        }

        private string GenerateJwtToken(AppUser user, int? customerId)
        {
            var jwtKey = _config["Jwt:Key"] ?? "VehiclePartsSecretKey2026!ForCoursework";
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name,  user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role,  user.Role)
            };

            if (customerId.HasValue)
            {
                claims.Add(new Claim("CustomerId", customerId.Value.ToString()));
            }

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
