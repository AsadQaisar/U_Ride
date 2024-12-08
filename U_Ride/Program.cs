using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using U_Ride.Data;
using U_Ride.Models;
using U_Ride.Services;

// var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS policy if needed (optional).
builder.Services.AddCors(options =>
{
    options.AddPolicy("reactApp", builder =>
    {
        builder.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3002", "http://localhost:8081")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// For Local Database
builder.Services.AddDbContext<ApplicationDbContext>(option => option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// For Cloud Database
//builder.Services.AddDbContext<ApplicationDbContext>(option => option.UseSqlServer(builder.Configuration.GetConnectionString("CloudConnection")));

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<RideService>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<SharedDb>();

// Add JWT authentication
var jwtSettings = builder.Configuration.GetSection("JWTSettings");
var tokenKey = jwtSettings["TokenKey"];
var key = Encoding.ASCII.GetBytes(tokenKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("reactApp");

//app.UseAuthentication();

app.UseAuthorization();

// Configure the SignalR hub
app.MapHub<ChatHub>("/Chat");

app.MapControllers();

app.Run();
