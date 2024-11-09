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
                var intervalPoints = await _rideService.CalculateIntervalPoints(ride.StartPoint, ride.EndPoint, intervals);

                // Create a list of route points for radius checking
                var routePoints = intervalPoints.Select(point => (point.Latitude, point.Longitude)).ToList();

                // Add the endpoint to the route points for radius checking
                var (stopLat, stopLon) = await _rideService.ParseCoordinates(ride.EndPoint);
                routePoints.Add((stopLat, stopLon));

                // Check if the student's endpoint falls within any of the driver's radius points
                if (await _rideService.IsPointWithinRouteRadius(coordinatesDto.EndPoint, routePoints, searchRadiusKm))
                {
                    matchingRides.Add(ride);
                }
            }

            return Ok(matchingRides);
        }


        [HttpGet("Ride_Calculation")]
        public async Task<IActionResult> RideCalculation([FromBody] RideDto.GeoCoordinatesDto coordinatesDto)
        {
            var distance = await _rideService.CalculateRouteDistanceAsync(coordinatesDto.StartPoint, coordinatesDto.EndPoint);
            if (distance is not null)
            {
                var decodedPoints = _rideService.DecodePolyline(distance.geometry);
                var intermediatePoints = _rideService.GetIntermediatePoints(decodedPoints, distance.summary.distance, 5);

                // Step 3: Add radius to each point
                var pointsWithRadius = intermediatePoints
                    .Select(point => (point.Lat, point.Lon, Radius: 2000))
                    .ToList();
                return Ok(pointsWithRadius);
            }
            return BadRequest();
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
