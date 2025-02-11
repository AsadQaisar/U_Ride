﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using U_Ride.Models;
using U_Ride.DTOs;
using U_Ride.Services;
using Microsoft.EntityFrameworkCore;
using static U_Ride.DTOs.RideDto;

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


        [HttpPost("PostRide")]
        [Authorize]
        public async Task<IActionResult> PostRide([FromBody] RideDto.PostRideDto postRideDto)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest(new { message = "User ID not found in token" });
            }

            var userId = Convert.ToInt32(userIdClaim.Value);

            var vehicle = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.UserID == userId);
            if (vehicle == null)
            {
                return BadRequest(new { message = "Vehicle not registered" });
            }

            int availableSeats = vehicle.SeatCapacity - 1;
            double baseRatePerKm = 10.0;
            // var distance = postRideDto.Distance ?? 0.0;
            // double price = _rideService.EstimatePrice(baseRatePerKm, distance, availableSeats);

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
                existingRide.Price = postRideDto.Price;
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
                    Price = postRideDto.Price,
                    CreatedOn = DateTime.UtcNow
                };
                await _context.Rides.AddAsync(newRide);
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Search Ride
            var students = await _context.Users
                .Where(h => h.HasVehicle == false)
                .Include(r => r.Ride)
                .AsNoTracking()
                .ToListAsync();

            // Filter to only available rides with encoded polyline
            var rides = students.Where(h => h.Ride != null && h.Ride.IsDriver == false && h.Ride.IsAvailable == true).ToList();

            // int intervals = 4;
            double searchRadiuskm = 2.0;

            var matchingRides = new List<UserInfo>();

            foreach (var ride in rides)
            {
                // Step 1: Decode Polyline
                var decodedPoints = _rideService.DecodePolyline(postRideDto.EncodedPolyline);

                // Step 2: Find the closest point within the search radius
                var endCoordinates = await _rideService.ParseCoordinates(ride.Ride.EndPoint);

                var closestPoint = _rideService.GetPointsWithinRadiusAndClosest(decodedPoints, endCoordinates, searchRadiuskm);

                // If a closest point within the radius is found, add the ride to matching rides
                if (closestPoint.PointsWithinRadius.Count != 0)
                {
                    // Create RideInfo object
                    var rideInfo = new RideInfo
                    {
                        RideID = ride.Ride.RideID,
                        RouteMatched = closestPoint.PointsWithinRadius.Count,
                        StartPoint = ride.Ride.StartPoint,
                        EndPoint = ride.Ride.EndPoint,
                        EncodedPolyline = ride.Ride.EncodedPolyline
                    };

                    // Create UserInfo object
                    var userInfo = new UserInfo
                    {
                        UserID = ride.UserID,
                        FullName = ride.FullName,
                        Gender = ride.Gender,
                        PhoneNumber = ride.PhoneNumber,
                        RideInfo = rideInfo,
                    };

                    // Add the UserInfo to the list of matching rides or process further
                    matchingRides.Add(userInfo);
                }
            }
            return Ok(matchingRides);
        }


        [HttpPost("CompleteRide")]
        [Authorize]
        public async Task<IActionResult> CompleteRide([FromQuery] int RideId)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest(new { message = "User ID not found in token." });
            }

            var userId = Convert.ToInt32(userIdClaim.Value);

            // Fetch the ride assigned to the driver
            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.RideID == RideId && r.UserID == userId);
            if (ride == null)
            {
                return NotFound(new { message = "Ride not found or unauthorized access." });
            }

            // Check if the ride is already completed
            if (ride.IsAvailable == false)
            {
                return BadRequest(new { message = "Ride is already marked as completed." });
            }

            // Update the ride status to completed
            ride.IsAvailable = false;

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
                return NotFound(new { message = "Ride or vehicle not found." });
            }

            var Ride = rideWithVehicle.Ride;
            var vehicle = rideWithVehicle.Vehicle;

            Ride.AvailableSeats = vehicle.SeatCapacity - 1;

            // Save changes to the database
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Ride marked as completed successfully.",
                RideID = RideId,
            });
        }


        [HttpPost("ConfirmRides")]
        [Authorize]
        public async Task<IActionResult> ConfirmRides([FromQuery] int RideId)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest(new { message = "User ID not found in token." });
            }

            var userId = Convert.ToInt32(userIdClaim.Value);

            // Fetch the ride assigned to the driver
            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.RideID == RideId && r.UserID == userId);
            if (ride == null)
            {
                return NotFound(new { message = "Ride not found or unauthorized access." });
            }

            ride.IsAvailable = false;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Ride Confirmed.", AvailableSeats = ride.AvailableSeats });
        }
    }
}
