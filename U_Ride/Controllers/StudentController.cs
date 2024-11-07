using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using U_Ride.DTOs;
using U_Ride.Models;
using U_Ride.Services;
using static U_Ride.DTOs.RideDto;

namespace U_Ride.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtTokenService _tokenService;
        private readonly RideService _rideService;

        // Optimized constructor
        public StudentController(JwtTokenService tokenService, ApplicationDbContext context, RideService rideService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context)); // Ensure dependencies are not null
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _rideService = rideService ?? throw new ArgumentNullException(nameof(rideService));
        }

        [HttpGet("SearchRides")]
        public async Task<IActionResult> SearchRides([FromBody] RideDto.GeoCoordinatesDto coordinatesDto)
        {
            var rides = await _context.Rides.Where(r => r.IsAvailable == true).ToListAsync();
            int intervals = 4;
            double searchRadiusKm = 4.0;

            var matchingRides = new List<Ride>();

            foreach (var ride in rides)
            {
                // Calculate interval points along the ride route
                var intervalPoints = _rideService.CalculateIntervalPoints(ride.StartPoint, ride.EndPoint, intervals);

                // Create a list of route points for radius checking
                var routePoints = intervalPoints.Select(point => (point.Latitude, point.Longitude)).ToList();

                // Add the endpoint to the route points for radius checking
                var (stopLat, stopLon) = _rideService.ParseCoordinates(ride.EndPoint);
                routePoints.Add((stopLat, stopLon));

                // Check if the student's endpoint falls within any of the driver's radius points
                if (_rideService.IsPointWithinRouteRadius(coordinatesDto.EndPoint, routePoints, searchRadiusKm))
                {
                    matchingRides.Add(ride);
                }
            }

            return Ok(matchingRides);
        }


        //[HttpPost("BookRide")]
        //public async Task<IActionResult> BookRide(int rideId, int studentId)
        //{
        //    var ride = await _context.Rides.FindAsync(rideId);
        //    if (ride == null || ride.Status == "Full") return NotFound("Ride not available.");

        //    // Deduct seat and update status
        //    ride.AvailableSeats -= 1;
        //    if (ride.AvailableSeats == 0)
        //        ride.Status = "Full";

        //    // Add booking
        //    var booking = new Booking
        //    {
        //        RideId = rideId,
        //        StudentId = studentId,
        //        BookingDate = DateTime.UtcNow
        //    };
        //    await _context.Bookings.AddAsync(booking);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { Message = "Booking successful.", RideId = rideId, AvailableSeats = ride.AvailableSeats });
        //}

    }
}
