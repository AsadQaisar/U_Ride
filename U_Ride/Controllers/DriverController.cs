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
        public async Task<IActionResult> PostRide([FromBody] RideDto.GeoCoordinatesDto coordinatesDto)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest("User ID not found in token");
            }

            var userId = Convert.ToInt32(userIdClaim.Value);
            var distance = _rideService.CalculateDistance(coordinatesDto.StartPoint, coordinatesDto.EndPoint);

            var vehicle = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.UserID == userId);
            if (vehicle == null)
            {
                return BadRequest("Vehicle not registered");
            }

            int availableSeats = vehicle.SeatCapacity - 1;
            double baseRatePerKm = 10.0;
            double price = _rideService.EstimatePrice(baseRatePerKm, await distance, availableSeats);

            // Check if a ride already exists for the user
            var existingRide = await _context.Rides.FirstOrDefaultAsync(r => r.DriverID == userId);

            if (existingRide != null)
            {
                // Update existing ride
                existingRide.StartPoint = coordinatesDto.StartPoint;
                existingRide.EndPoint = coordinatesDto.EndPoint;
                existingRide.AvailableSeats = availableSeats;
                existingRide.Price = price;
                existingRide.LastModifiedOn = DateTime.UtcNow; 
            }
            else
            {
                // Create new ride
                var newRide = new Ride
                {
                    DriverID = userId,
                    StartPoint = coordinatesDto.StartPoint,
                    EndPoint = coordinatesDto.EndPoint,
                    AvailableSeats = availableSeats,
                    Price = price,
                    CreatedOn = DateTime.UtcNow
                };
                await _context.Rides.AddAsync(newRide);
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Ride posted successfully." });


            //// Step 1: Calculate distance between start and stop points
            //// var distance = _distanceService.CalculateDistance(coordinatesDto.StartLatitude, coordinatesDto.StartLongitude, coordinatesDto.StopLatitude, coordinatesDto.StopLongitude);
            //var distance = _rideService.CalculateDistance(coordinatesDto.StartPoint, coordinatesDto.StopPoint);

            //// var points = _distanceService.CalculateIntervalPoints(coordinatesDto.StartLatitude, coordinatesDto.StartLongitude, coordinatesDto.StopLatitude, coordinatesDto.StopLongitude, 4);
            //// Step 2: Divide the route into interval points, excluding the stop point
            //int intervals = 4;
            //var intervalPoints = _rideService.CalculateIntervalPoints(coordinatesDto.StartPoint, coordinatesDto.StopPoint, intervals);


            //// Step 3: Create 4-km radius around each interval point
            //var radiusPoints = intervalPoints.Select(point => new
            //{
            //    Latitude = point.Latitude,
            //    Longitude = point.Longitude,
            //    RadiusKm = 4
            //}).ToList();

            //// Step 4: Add the stop point with a 4-km radius
            //var (stopLat, stopLon) = _rideService.ParseCoordinates(coordinatesDto.StopPoint);
            //radiusPoints.Add(new
            //{
            //    Latitude = stopLat,
            //    Longitude = stopLon,
            //    RadiusKm = 4
            //});

            //// Return the interval points and stop point with their radii
            //return Ok(radiusPoints);
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
