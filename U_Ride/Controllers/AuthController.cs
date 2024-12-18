﻿using Microsoft.AspNetCore.Authorization;
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
                    return NotFound("User not found.");
                }

                // Check if another user has the same SeatNumber or PhoneNumber
                var isDuplicate = await _context.Users
                    .AnyAsync(u => (u.UserID != registerDto.UserID) // Exclude the current user
                    && (u.SeatNumber == registerDto.SeatNumber || u.PhoneNumber == registerDto.PhoneNumber));

                if (isDuplicate)
                {
                    return Conflict("Seat Number or Phone Number is already in use by another user.");
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
                    return Conflict("User with the same Seat Number or Phone Number already exists.");
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

            return Ok("User information saved successfully.");
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
                return Unauthorized("Invalid credentials or inactive account.");
            }

            // Fetch associated vehicle info if the user has one
            AuthDto.VehicleInfo? vehicleInfo = null;
            if (user.HasVehicle)
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
                Vehicle = vehicleInfo,
                Authorization = new Authorization
                {
                    token = token
                }
            };

            // Return the response
            return Ok(userInfo);
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


        [HttpGet("UserInfo")]
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
