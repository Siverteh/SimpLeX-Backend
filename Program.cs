using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimpLeX_Backend.Services;
using SimpLeX_Backend.Data;
using SimpLeX_Backend.Models;
using Microsoft.Extensions.Configuration; // Import the IConfiguration namespace
using Microsoft.IdentityModel.Tokens; // For TokenValidationParameters
using Microsoft.AspNetCore.Authentication.JwtBearer; // For JWT Bearer
using Microsoft.AspNetCore.Authorization;
using System.Text; // For Encoding
using System;
using System.Security.Cryptography;


var builder = WebApplication.CreateBuilder(args);

// Construct the connection string from environment variables
var server = Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "localhost";
var port = Environment.GetEnvironmentVariable("DATABASE_PORT") ?? "5432"; // Default PostgreSQL port
var database = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "simplex_db";
var username = Environment.GetEnvironmentVariable("DATABASE_USER") ?? "postgres";
var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "skallesverd5"; // Make sure to replace 'changeit' with your real password

var connectionString = $"Host={server};Port={port};Database={database};Username={username};Password={password};";

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Use the constructed connection string to add your DbContext with Npgsql for PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure ASP.NET Core Identity to use your User model and your custom DbContext
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<TokenService>();

// Register the IDocumentService with an HttpClient configured to use the base address of your service.
builder.Services.AddHttpClient<DocumentService>(client =>
{
    client.BaseAddress = new Uri("http://simplex-compiler-service:8080"); // Assuming you have this URL in your configuration
});

// Retrieve the JWTKey from appsettings.json
var jwtKey = builder.Configuration.GetValue<string>("JwtKey") ?? throw new InvalidOperationException("JWT Key is not configured.");

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ClockSkew = TimeSpan.Zero,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "apiWithAuthBackend",
            ValidAudience = "apiWithAuthBackend",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

var app = builder.Build();

// Automatically apply any pending migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate(); // This will apply all pending migrations
}

// Swagger configuration
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SimpLeX API v1"));

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();
