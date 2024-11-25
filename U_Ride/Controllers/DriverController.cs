using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using U_Ride.Models;
using U_Ride.DTOs;
using U_Ride.Services;
using Microsoft.EntityFrameworkCore;

namespace U_Ride.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DriverController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtTokenService _tokenService;
        private readonly RideService _rideService;

        // Optimized constructor
        public DriverController(JwtTokenService tokenService, ApplicationDbContext context, RideService rideService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context)); // Ensure dependencies are not null
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _rideService = rideService ?? throw new ArgumentNullException(nameof(rideService));
        }

        [HttpPut("PostRide")]
        [Authorize]
        public async Task<IActionResult> PostRide([FromBody] RideDto.PostRideDto postRideDto)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest("User ID not found in token");
            }

            var userId = Convert.ToInt32(userIdClaim.Value);

            var vehicle = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.UserID == userId);
            if (vehicle == null)
            {
                return BadRequest("Vehicle not registered");
            }

            int availableSeats = vehicle.SeatCapacity - 1;
            double baseRatePerKm = 10.0;
            var distance = postRideDto.Distance ?? 0.0;
            double price = _rideService.EstimatePrice(baseRatePerKm, distance, availableSeats);

            // Check if a ride already exists for the user
            var existingRide = await _context.Rides.FirstOrDefaultAsync(r => r.UserID == userId);

            if (existingRide != null)
            {
                // Update existing ride
                existingRide.StartPoint = postRideDto.StartPoint;
                existingRide.EndPoint = postRideDto.EndPoint;
                existingRide.EncodedPolyline = postRideDto.EncodedPolyline;
                existingRide.Distance = postRideDto.Distance;
                existingRide.AvailableSeats = availableSeats;
                existingRide.Price = price;
                existingRide.LastModifiedOn = DateTime.UtcNow; 
            }
            else
            {
                // Create new ride
                var newRide = new Ride
                {
                    UserID = userId,
                    StartPoint = postRideDto.StartPoint,
                    EndPoint = postRideDto.EndPoint,
                    EncodedPolyline = postRideDto.EncodedPolyline,
                    Distance = postRideDto.Distance,
                    AvailableSeats = availableSeats,
                    Price = price,
                    CreatedOn = DateTime.UtcNow
                };
                await _context.Rides.AddAsync(newRide);
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Ride posted successfully." });

        }

        [HttpPost("UpdateSeats")]
        public async Task<IActionResult> UpdateSeats(int rideId)
        {
            var ride = await _context.Rides.FindAsync(rideId);
            if (ride == null || ride.AvailableSeats <= 0) return NotFound("Ride not available");

            ride.AvailableSeats -= 1;

            if (ride.AvailableSeats == 0)
                ride.IsAvailable = false;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Seat updated.", AvailableSeats = ride.AvailableSeats });
        }
    }
}
