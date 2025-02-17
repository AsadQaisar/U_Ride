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
using static U_Ride.DTOs.RideDto;

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

            User user;

            if (registerDto.UserID != null)
            {
                // Find existing user by ID
                user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == registerDto.UserID);

                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                // Check if another user has the same SeatNumber or PhoneNumber
                var isDuplicate = await _context.Users
                    .AnyAsync(u => (u.UserID != registerDto.UserID) // Exclude the current user
                    && (u.SeatNumber == registerDto.SeatNumber || u.PhoneNumber == registerDto.PhoneNumber));

                if (isDuplicate)
                {
                    // Error 409
                    return Conflict(new { message = "Seat Number or Phone Number is already in use by another user." });
                }

                // Update user details
                user.FullName = registerDto.FullName;
                user.Gender = registerDto.Gender;
                user.SeatNumber = registerDto.SeatNumber;
                user.Department = registerDto.Department;
                user.PhoneNumber = registerDto.PhoneNumber;
                user.LastModifiedOn = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(registerDto.Password))
                {
                    user.Password = _passwordHasher.HashPassword(user, registerDto.Password);
                }
            }
            else
            {
                // Check if SeatNumber or PhoneNumber already exists
                var existingUser = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.SeatNumber == registerDto.SeatNumber || u.PhoneNumber == registerDto.PhoneNumber);

                if (existingUser != null)
                {
                    // Error 409
                    return Conflict(new { message = "User with the same Seat Number or Phone Number already exists." });
                }

                // Create new user
                user = new User
                {
                    FullName = registerDto.FullName,
                    Gender = registerDto.Gender,
                    SeatNumber = registerDto.SeatNumber,
                    Department = registerDto.Department,
                    PhoneNumber = registerDto.PhoneNumber,
                    CreatedOn = DateTime.UtcNow,
                    LastModifiedOn = DateTime.UtcNow,
                    IsActive = true,
                    Password = _passwordHasher.HashPassword(new User(), registerDto.Password)
                };
                _context.Users.Add(user);
            }
            await _context.SaveChangesAsync();

            return Ok(new { message = "User information saved successfully." });
        }


        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] AuthDto.LoginDto loginDto)
        {
            // Validate user credentials
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.SeatNumber == loginDto.Phone_SeatNumber || u.PhoneNumber == loginDto.Phone_SeatNumber);

            if (user == null || !user.IsActive ||
                _passwordHasher.VerifyHashedPassword(user, user.Password, loginDto.Password) == PasswordVerificationResult.Failed)
            {
                // Error 409
                return Conflict(new { message = "Invalid credentials or inactive account." });
            }

            // Fetch associated vehicle info if the user has one
            AuthDto.VehicleInfo? vehicleInfo = null;
            if (user.HasVehicle == true)
            {
                var vehicle = await _context.Vehicles.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.UserID == user.UserID);

                if (vehicle != null)
                {
                    vehicleInfo = new AuthDto.VehicleInfo
                    {
                        VehicleType = vehicle.VehicleType,
                        Make_Model = $"{vehicle.Make} {vehicle.Model}",
                        LicensePlate = vehicle.LicensePlate
                    };
                }
            }

            // Fetch chat IDs of the user
            var chatIds = await _context.Chats
                .AsNoTracking()
                .Where(c => c.ReceiverID == user.UserID || c.SenderID == user.UserID)
                .Select(c => c.ChatID)
                .ToListAsync();

            // Generate the token for the user
            var token = _tokenService.GenerateToken(user); // Replace with your actual token generation logic

            // Prepare the response DTO
            var userInfo = new AuthDto.UserInfo
            {
                UserID = user.UserID,
                FullName = user.FullName,
                SeatNumber = user.SeatNumber,
                Gender = user.Gender,
                PhoneNumber = user.PhoneNumber,
                ChatIDs = chatIds,
                Vehicle = vehicleInfo,
                Authorization = new Authorization
                {
                    token = token
                }
            };

            // Return the response
            return Ok(userInfo);
        }


        [HttpPost("IsDriver")]
        [Authorize]
        public async Task<IActionResult> IsDriver([FromQuery] bool hasVehicle)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest(new { message = "User ID not found in token" });
            }

            var userId = Convert.ToInt32(userIdClaim.Value);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            user.HasVehicle = hasVehicle;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Fetch vehicle details for return
            var vehicleInfo = await _context.Vehicles
                .AsNoTracking()
                .Where(v => v.UserID == userId)
                .Select(v => new
                {
                    v.VehicleType,
                    v.Make,
                    v.Model,
                    v.Color,
                    v.Year,
                    v.LicensePlate,
                    v.SeatCapacity
                })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                message = "User information updated.",
                vehicleInfo = vehicleInfo ?? null,
                isDriver = hasVehicle
            });
        }


        [HttpPut("RegisterVehicle")]
        [Authorize]
        public async Task<IActionResult> RegisterVehicle([FromBody] AuthDto.RegVehicleDto reqVehicleDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest(new { message = "User ID not found in token" });
            }

            var userId = Convert.ToInt32(userIdClaim.Value);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.HasVehicle = true;
            _context.Users.Update(user);

            // If the user is a driver, save vehicle details
            if (user.HasVehicle == true)
            {
                // Validate seat capacity based on vehicle type
                if ((reqVehicleDto.VehicleType.ToLower() == "bike" && reqVehicleDto.SeatCapacity > 1) ||
                    (reqVehicleDto.VehicleType.ToLower() == "car" && reqVehicleDto.SeatCapacity > 4))
                {
                    return BadRequest(new { message = "Invalid seat capacity for the selected vehicle type." });
                }

                var existingVehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.UserID == userId);
                if (existingVehicle != null)
                {
                    // Update existing vehicle record
                    existingVehicle.Make = reqVehicleDto.Make;
                    existingVehicle.Model = reqVehicleDto.Model;
                    existingVehicle.LicensePlate = reqVehicleDto.LicensePlate;
                    existingVehicle.Year = reqVehicleDto.Year;
                    existingVehicle.Color = reqVehicleDto.Color;
                    existingVehicle.VehicleType = reqVehicleDto.VehicleType;
                    existingVehicle.SeatCapacity = reqVehicleDto.SeatCapacity;
                    existingVehicle.LastModifiedOn = DateTime.UtcNow;

                    _context.Vehicles.Update(existingVehicle);
                }
                else
                {
                    // Add new vehicle record
                    var newVehicle = new Vehicle
                    {
                        UserID = userId,
                        Make = reqVehicleDto.Make,
                        Model = reqVehicleDto.Model,
                        LicensePlate = reqVehicleDto.LicensePlate,
                        Year = reqVehicleDto.Year,
                        Color = reqVehicleDto.Color,
                        VehicleType = reqVehicleDto.VehicleType,
                        SeatCapacity = reqVehicleDto.SeatCapacity,
                        CreatedOn = DateTime.UtcNow
                    };
                    await _context.Vehicles.AddAsync(newVehicle);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "User and vehicle information updated." });
        }


        [HttpGet("UserInfo")]
        [Authorize]
        public async Task<IActionResult> UserInfo()
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest(new { message = "User ID not found in token" });
            }

            var userId = Convert.ToInt32(userIdClaim.Value);
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Check if user has a vehicle and include vehicle information if available
            object userInfo;

            if (user.HasVehicle == true)
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

        [Authorize]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User ID not found in token." });
            }

            int userId = int.Parse(userIdClaim.Value);
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (!string.IsNullOrEmpty(token))
            {
                _context.BlacklistedTokens.Add(new BlacklistedToken
                {
                    UserID = userId,
                    Token = token,
                    RevokedOn = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Logged out successfully." });
        }

    }
}
