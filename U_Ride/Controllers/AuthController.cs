using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using U_Ride.DTOs;
using U_Ride.Models;
using U_Ride.Services;
using static U_Ride.DTOs.AuthDto;

namespace U_Ride.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly JwtTokenService _tokenService;

        public AuthController(JwtTokenService tokenService, ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _tokenService = tokenService;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] AuthDto.RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if SeatNumber or PhoneNumber already exists
            var existingUser = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.SeatNumber == registerDto.SeatNumber || u.PhoneNumber == registerDto.PhoneNumber);

            if (existingUser != null)
            {
                return Conflict("User with the same Seat Number or Phone Number already exists.");
            }

            var user = new User
            {
                FullName = registerDto.FullName,
                Gender = registerDto.Gender,
                SeatNumber = registerDto.SeatNumber,
                Department = registerDto.Department,
                PhoneNumber = registerDto.PhoneNumber,
                CreatedOn = DateTime.UtcNow,
                LastModifiedOn = DateTime.UtcNow,
                IsActive = true
            };
            user.Password = _passwordHasher.HashPassword(user, registerDto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully.");
        }


        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] AuthDto.LoginDto loginDto)
        {
            // Validate user credentials...
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.SeatNumber == loginDto.Phone_SeatNumber || u.PhoneNumber == loginDto.Phone_SeatNumber);

            if (user == null || user.IsActive == false || _passwordHasher.VerifyHashedPassword(user, user.Password, loginDto.Password) == PasswordVerificationResult.Failed)
            {
                return Unauthorized();
            }

            var token = _tokenService.GenerateToken(user);
            return Ok(new { Token = token });
        }
       

        [HttpPut("IsDriver")]
        [Authorize]
        public async Task<IActionResult> SetDriverStatus([FromBody] AuthDto.IsDriverDto isDriverDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest("User ID not found in token");
            }

            var userId = Convert.ToInt32(userIdClaim.Value);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            user.HasVehicle = true;
            _context.Users.Update(user);

            // If the user is a driver, save vehicle details
            if (user.HasVehicle)
            {
                var existingVehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.UserID == userId);
                if (existingVehicle != null)
                {
                    // Update existing vehicle record
                    existingVehicle.Make = isDriverDto.Make;
                    existingVehicle.Model = isDriverDto.Model;
                    existingVehicle.LicensePlate = isDriverDto.LicensePlate;
                    existingVehicle.Year = isDriverDto.Year;
                    existingVehicle.Color = isDriverDto.Color;
                    existingVehicle.VehicleType = isDriverDto.VehicleType;
                    existingVehicle.SeatCapacity = isDriverDto.SeatCapacity;
                    existingVehicle.LastModifiedOn = DateTime.UtcNow;

                    _context.Vehicles.Update(existingVehicle);
                }
                else
                {
                    // Add new vehicle record
                    var newVehicle = new Vehicle
                    {
                        UserID = userId,
                        Make = isDriverDto.Make,
                        Model = isDriverDto.Model,
                        LicensePlate = isDriverDto.LicensePlate,
                        Year = isDriverDto.Year,
                        Color = isDriverDto.Color,
                        VehicleType = isDriverDto.VehicleType,
                        SeatCapacity = isDriverDto.SeatCapacity,
                        CreatedOn = DateTime.UtcNow
                    };
                    await _context.Vehicles.AddAsync(newVehicle);
                }
            }

            await _context.SaveChangesAsync();

            return Ok("User and vehicle information updated.");
        }


        [HttpGet("Userinfo")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest("User ID not found in token");
            }

            var userId = Convert.ToInt32(userIdClaim.Value);
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Check if user has a vehicle and include vehicle information if available
            object userInfo;

            if (user.HasVehicle)
            {
                var vehicle = await _context.Vehicles.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.UserID == userId);

                userInfo = new
                {
                    user.FullName,
                    user.SeatNumber,
                    user.Department,
                    user.PhoneNumber,
                    user.Gender,
                    user.HasVehicle,
                    user.CreatedOn,
                    Vehicle = vehicle != null ? new
                    {
                        vehicle.Make,
                        vehicle.Model,
                        vehicle.LicensePlate,
                        vehicle.Year,
                        vehicle.Color,
                        vehicle.VehicleType,
                        vehicle.SeatCapacity,
                        vehicle.CreatedOn,
                        vehicle.LastModifiedOn
                    } : null
                };
            }
            else
            {
                userInfo = new
                {
                    user.FullName,
                    user.SeatNumber,
                    user.Department,
                    user.PhoneNumber,
                    user.Gender,
                    user.HasVehicle,
                    user.CreatedOn
                };
            }

            return Ok(userInfo);

        }
    }
}
