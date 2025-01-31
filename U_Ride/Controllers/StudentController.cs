using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IHubContext<ChatHub> _hubContext;

        // Optimized constructor
        public StudentController(JwtTokenService tokenService, ApplicationDbContext context, RideService rideService, IHubContext<ChatHub> hubContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context)); // Ensure dependencies are not null
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _rideService = rideService ?? throw new ArgumentNullException(nameof(rideService));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }


        [HttpPost("SearchRides")]
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

            var matchingRides = new List<UserInfo>();

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

                    var rideInfo = new RideInfo
                    {
                        RideID = ride.Ride.RideID,
                        RouteMatched = closestPoint.PointsWithinRadius.Count,
                        StartPoint = ride.Ride.StartPoint,
                        EndPoint = ride.Ride.EndPoint,
                        EncodedPolyline = ride.Ride.EncodedPolyline,
                        Price = ride.Ride.Price.ToString(),
                        AvailableSeats = ride.Ride.AvailableSeats.ToString()
                    };
                    var vehicleInfo = new VehicleInfo
                    {
                        VehicleType = ride.Vehicle.VehicleType,
                        Make_Model = ($"{ride.Vehicle.Color} {ride.Vehicle.Make} {ride.Vehicle.Model}"),
                        Color = ride.Vehicle.Color,
                        LicensePlate = ride.Vehicle.LicensePlate
                    };

                    var userInfo = new UserInfo
                    {
                        UserID = ride.UserID,
                        FullName = ride.FullName,
                        Gender = ride.Gender,
                        PhoneNumber = ride.PhoneNumber,
                        VehicleInfo = vehicleInfo,
                        RideInfo = rideInfo,
                    };

                    matchingRides.Add(userInfo);
                }
            }
            return Ok(matchingRides);
        }


        [HttpPost("BookRide")]
        [Authorize]
        public async Task<IActionResult> BookRide([FromQuery] int RideId)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest("User ID not found in token");
            }

            var userId = Convert.ToInt32(userIdClaim.Value);

            var ride = await _context.Rides.FindAsync(RideId);
            if (ride == null || ride.IsAvailable == false) return NotFound("Ride not available.");

            // Deduct seat and update status
            ride.AvailableSeats -= 1;
            if (ride.AvailableSeats == 0)
                ride.IsAvailable = false;

            // Add booking
            var booking = new Booking
            {
                RideID = RideId,
                UserID = Convert.ToInt16(userId),
                BookingDate = DateTime.UtcNow
            };
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();

            // Send the message to the receiver's group
            await _hubContext.Clients.Group(ride.UserID.ToString())
                .SendAsync("ReceiveMessage", userId, "This user booked your ride.");
            return Ok(new { Message = "Booking successful.", RideID = RideId, AvailableSeats = ride.AvailableSeats });
        }


        [HttpPost("CancelRide")]
        [Authorize]
        public async Task<IActionResult> CancelRide([FromQuery] int RideId)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest("User ID not found in token.");
            }

            var userId = Convert.ToInt32(userIdClaim.Value);

            // Find the booking for the given user and ride
            var booking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.RideID == RideId && b.UserID == userId);
            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            var rideWithVehicle = await (from r in _context.Rides
                                         join v in _context.Vehicles on r.UserID equals v.UserID
                                         where r.RideID == RideId
                                         select new
                                         {
                                             Ride = r,
                                             Vehicle = v
                                         }).FirstOrDefaultAsync();

            if (rideWithVehicle == null)
            {
                return NotFound("Ride or vehicle not found.");
            }

            var ride = rideWithVehicle.Ride;
            var vehicle = rideWithVehicle.Vehicle;

            // Ensure the available seats don't exceed the total seat capacity of the vehicle
            if (ride.AvailableSeats + 1 > vehicle.SeatCapacity - 1)
            {
                return BadRequest("Seat count exceeds the vehicle's capacity.");
            }

            // Remove the booking and update ride availability
            ride.AvailableSeats += 1;

            if (ride.AvailableSeats > 0)
            {
                ride.IsAvailable = true;
            }

            await _context.SaveChangesAsync();

            // Send the message to the receiver's group
            await _hubContext.Clients.Group(ride.UserID.ToString())
                .SendAsync("ReceiveMessage", userId, "This user canceled ride with you.");
            return Ok(new { Message = "Ride canceled successfully.", RideID = RideId, AvailableSeats = ride.AvailableSeats });
        }


        [HttpGet("Ride_Calculation")]
        public async Task<IActionResult> Ride_Calculation([FromBody] RideDto.PostRideDto postRideDto)
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
    }
}
