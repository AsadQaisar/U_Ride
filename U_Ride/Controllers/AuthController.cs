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

            if (user == null || _passwordHasher.VerifyHashedPassword(user, user.Password, loginDto.Password) == PasswordVerificationResult.Failed)
            {
                return Unauthorized();
            }

            var token = _tokenService.GenerateToken(user);
            return Ok(new { Token = token });
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

            var userId = userIdClaim.Value;
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == Convert.ToInt16(userId));
            if (user == null)
            {
                return NotFound("User not found");
            }

            var userInfo = new
            {
                user.FullName,
                user.SeatNumber,
                user.Department,
                user.PhoneNumber,
                user.Gender,
                user.CreatedOn
            };

            return Ok(userInfo);
        }
    }
}
