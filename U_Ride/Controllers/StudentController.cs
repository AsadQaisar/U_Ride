using Microsoft.AspNetCore.Authorization;
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
        [Authorize]
        public async Task<IActionResult> SearchRides([FromBody] RideDto.PostRideDto postRideDto)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest("User ID not found in token");
            }

            var userId = Convert.ToInt32(userIdClaim.Value);

            // Check if a ride already exists for the user
            var existingRide = await _context.Rides.FirstOrDefaultAsync(r => r.UserID == userId);

            if (existingRide != null)
            {
                // Update existing ride
                existingRide.StartPoint = postRideDto.StartPoint;
                existingRide.EndPoint = postRideDto.EndPoint;
                existingRide.EncodedPolyline = postRideDto.EncodedPolyline;
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
                    IsDriver = false,
                    CreatedOn = DateTime.UtcNow
                };
                await _context.Rides.AddAsync(newRide);
            }
            await _context.SaveChangesAsync();

            // Search Ride
            var drivers = await _context.Users
                .Where(h => h.HasVehicle == true)  
                .Include(r => r.Ride)              
                .Include(v => v.Vehicle)          
                .AsNoTracking()
                .ToListAsync();

            // Filter to only available rides with encoded polyline
            var rides = drivers.Where(h => h.Ride != null && h.Ride.IsDriver == true && h.Ride.IsAvailable == true).ToList();

            // int intervals = 4;
            double searchRadiuskm = 2.0;

            var matchingRides = new List<Root>(); 

            foreach (var ride in rides)
            {
                // Step 1: Decode Polyline
                var decodedPoints = _rideService.DecodePolyline(ride.Ride.EncodedPolyline);

                // Step 2: Find the closest point within the search radius
                var endCoordinates = await _rideService.ParseCoordinates(postRideDto.EndPoint);

                var closestPoint = _rideService.GetPointsWithinRadiusAndClosest(decodedPoints, endCoordinates, searchRadiuskm);

                // If a closest point within the radius is found, add the ride to matching rides
                if (closestPoint.PointsWithinRadius.Count != 0)
                {
                    var driverInfo = new UserInfo
                    {
                        UserID = ride.UserID,
                        FullName = ride.FullName,
                        Gender = ride.Gender,
                        PhoneNumber = ride.PhoneNumber
                    };
                    var rideInfo = new RideInfo
                    {
                        RouteMatched = closestPoint.PointsWithinRadius.Count,
                        Price = ride.Ride.Price.ToString(), 
                        AvailableSeats = ride.Ride.AvailableSeats.ToString()
                    };
                    var vehicleInfo = new VehicleInfo
                    {
                        VehicleType = ride.Vehicle.VehicleType,
                        Make_Model = ($"{ ride.Vehicle.Color} {ride.Vehicle.Make} {ride.Vehicle.Model}"),
                        LicensePlate = ride.Vehicle.LicensePlate
                    };
                    var root = new Root
                    {
                        RideInfo = rideInfo,
                        UserInfo = driverInfo,
                        VehicleInfo = vehicleInfo
                    };

                    matchingRides.Add(root);
                }
            }
            return Ok(matchingRides);
        }


        [HttpGet("Ride_Calculation")]
        public async Task<IActionResult> RideCalculation([FromBody] RideDto.PostRideDto postRideDto)
        {
            var distance = await _rideService.CalculateRouteDistanceAsync(postRideDto.StartPoint, postRideDto.EndPoint);
            if (distance is not null)
            {
                var decodedPoints = _rideService.DecodePolyline(distance.geometry);
                var encodedPolyline = _rideService.EncodePolyline(decodedPoints);
                // var geometry = await _rideService.GetRouteGeoJsonAsync(decodedPoints);

                // Verfying Encoded Polylone
                // ======================================================================================
                //var startPoint = $"{decodedPoints[0].Lat},{decodedPoints[0].Lon}";
                //var endPoint = $"{decodedPoints[^1].Lat},{decodedPoints[^1].Lon}";

                //var routeInfo = await _rideService.CalculateRouteDistanceAsync(startPoint, endPoint);
                // ======================================================================================

                var intermediatePoints = _rideService.GetIntermediatePoints(decodedPoints, distance.summary.distance, 5);

                // Step 3: Add radius to each point
                var pointsWithRadius = intermediatePoints
                    .Select(point => (point.Lat, point.Lon, Radius: 2000))
                    .ToList();
                return Ok(distance);
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

        //[NonAction]
        //public void PopulateUser()
        //{
        //    var Tbl_User = _context.Categories.Where(m => m.UserId == Global_Variables.LoginID).ToList();
        //    var CategoryCollection = _context.Categories.ToList();
        //    Category DefaultCategory = new Category() { CategoryId = 0, Title = "Choose a Category" };
        //    Tbl_User.Insert(0, DefaultCategory);
        //    ViewBag.Categories = Tbl_User;
        //}
    }
}
