using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VehiclePartsBackend.Data;

var builder = WebApplication.CreateBuilder(args);

// ── JWT Configuration ──────────────────────────────────────────────
var jwtKey     = builder.Configuration["Jwt:Key"]      ?? "VehiclePartsSecretKey2026!ForCoursework";
var jwtIssuer  = builder.Configuration["Jwt:Issuer"]   ?? "VehiclePartsBackend";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "VehiclePartsFrontend";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Prevent circular reference errors (e.g. Review -> Customer -> Review)
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddSingleton<VehiclePartsBackend.Services.IInvoiceEmailService, VehiclePartsBackend.Services.InvoiceEmailService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHostedService<VehiclePartsBackend.Services.NotificationBackgroundService>();

// ── PostgreSQL Database Connection ────────────────────────────────────
// Make sure "DefaultConnection" in appsettings.json has your correct password
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
// ─────────────────────────────────────────────────────────────────────

// ── CORS: allow React frontend at localhost:5173–5180 ──────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:5175",
                "http://localhost:5176",
                "http://localhost:5177"
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
