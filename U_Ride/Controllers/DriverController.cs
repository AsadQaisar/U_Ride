using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using U_Ride.Models;
using U_Ride.DTOs;
using U_Ride.Services;

namespace U_Ride.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DriverController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtTokenService _tokenService;
        private readonly DistanceService _distanceService;
        private readonly PasswordHasher<User> _passwordHasher;

        // Optimized constructor
        public DriverController(JwtTokenService tokenService, ApplicationDbContext context, DistanceService distanceService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context)); // Ensure dependencies are not null
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _distanceService = distanceService ?? throw new ArgumentNullException(nameof(distanceService));

            // PasswordHasher does not need to be re-initialized every time, so it's fine to keep it as a class-level variable
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpPost("PostRide")]
        // [Authorize]
        public IActionResult PostRide([FromBody] RideDto.GeoCoordinatesDto coordinatesDto)
        {
            // Step 1: Calculate distance between start and stop points
            // var distance = _distanceService.CalculateDistance(coordinatesDto.StartLatitude, coordinatesDto.StartLongitude, coordinatesDto.StopLatitude, coordinatesDto.StopLongitude);
            var distance = _distanceService.CalculateDistance(coordinatesDto.StartPoint, coordinatesDto.StopPoint);

            // Step 2: Divide the route into 4 points along the route
            var intervalDistance = distance / 4;
            // var points = _distanceService.CalculateIntervalPoints(coordinatesDto.StartLatitude, coordinatesDto.StartLongitude, coordinatesDto.StopLatitude, coordinatesDto.StopLongitude, 4);
            var points = _distanceService.CalculateIntervalPoints(coordinatesDto.StartPoint, coordinatesDto.StopPoint, 4);

            // Step 3: Create 4-km radius around each interval point
            var radiusPoints = points.Select(point => new
            {
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                RadiusKm = 4
            }).ToList();

            // Return the points and their radii
            return Ok(radiusPoints);
        }

        
    }
}
