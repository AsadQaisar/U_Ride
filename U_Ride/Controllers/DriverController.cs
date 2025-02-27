﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using U_Ride.Models;
using U_Ride.DTOs;
using U_Ride.Services;
using Microsoft.EntityFrameworkCore;
using static U_Ride.DTOs.RideDto;
using Microsoft.AspNetCore.SignalR;

namespace U_Ride.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DriverController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtTokenService _tokenService;
        private readonly RideService _rideService;
        private readonly IHubContext<ChatHub> _hubContext;

        // Optimized constructor
        public DriverController(JwtTokenService tokenService, ApplicationDbContext context, RideService rideService, IHubContext<ChatHub> hubContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context)); // Ensure dependencies are not null
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _rideService = rideService ?? throw new ArgumentNullException(nameof(rideService));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));

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
                existingRide.IsAvailable = true;
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
                    IsAvailable = true,
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


        [HttpPost("AcceptRide")]
        [Authorize]
        public async Task<IActionResult> AcceptRide([FromBody] RejectMessagesDto rejectMessagesDto)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest(new { message = "User ID not found in token." });
            }

            int userId = Convert.ToInt32(userIdClaim.Value);

            // Fetch user, driver ride, and passenger ride in a single query to reduce DB hits
            var userRides = await _context.Rides
                .Where(r => r.UserID == userId || r.UserID == rejectMessagesDto.PassengerId)
                .ToListAsync();

            var driverRide = userRides.FirstOrDefault(r => r.UserID == userId);
            
            // Fetch passenger's ride details
            var passengerRide = await _context.Rides
                .AsNoTracking()
                .Where(r => r.UserID == rejectMessagesDto.PassengerId)
                .Select(r => new
                {
                    RideID = r.RideID,
                    StartPoint = r.StartPoint,
                    EndPoint = r.EndPoint,
                    EncodedPolyline = r.EncodedPolyline,
                    Distance = r.Distance,
                    Price = r.Price,
                    IsAvailable = r.IsAvailable
                })
                .FirstOrDefaultAsync();

            if (driverRide == null || passengerRide == null)
            {
                return NotFound(new { message = "Ride not found or unauthorized access." });
            }

            // Avoid assignment inside the condition
            if (!driverRide.IsAvailable || driverRide.AvailableSeats <= 0)
            {
                return BadRequest(new { message = "Seats full or Ride unavailable." });
            }

            if (!passengerRide.IsAvailable)
            {
                return BadRequest(new { message = "Passenger unavailable." });
            }

            // Deduct seat and update status
            driverRide.AvailableSeats--;

            if (driverRide.AvailableSeats == 0)
            {
                driverRide.IsAvailable = false;

                // Notify all chat IDs in rejectMessagesDto.ChatIds that the seats are full
                foreach (var userID in rejectMessagesDto.UserIDs)
                {
                    if (userID != rejectMessagesDto.PassengerId)
                    {
                        var chat = await _context.Chats.Include(m => m.Messages).FirstOrDefaultAsync(c =>
                            (c.ReceiverID == userID && c.SenderID == Convert.ToInt16(userId)) ||
                            (c.ReceiverID == Convert.ToInt16(userId) && c.SenderID == userID)
                        );

                        if (chat != null)
                        {
                            // Delete Chat
                            _context.Chats.Remove(chat);
                            await _context.SaveChangesAsync();
                        }

                        await _hubContext.Clients.Group(userID.ToString())
                            .SendAsync("RideStatus", null, "The driver's seats are now fully booked.");
                    }
                }
            }

            // Add booking
            var booking = new Booking
            {
                RideID = driverRide.RideID,
                UserID = userId,
                PassengerID = rejectMessagesDto.PassengerId,
                BookingDate = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Fetch user details along with vehicle information
            var driverinfo = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserID == userId)
                .Select(u => new
                {
                    UserID = u.UserID,
                    FullName = u.FullName,
                    SeatNumber = u.SeatNumber,
                    Gender = u.Gender,
                    PhoneNumber = u.PhoneNumber,
                    Vehicle = _context.Vehicles
                        .Where(v => v.UserID == u.UserID)
                        .Select(v => new
                        {
                            v.VehicleType,
                            v.Make,
                            v.Model,
                            v.Color,
                            v.LicensePlate
                        }).FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            // Send the message to the receiver's group with user and vehicle info
            await _hubContext.Clients.Group(rejectMessagesDto.PassengerId.ToString())
                .SendAsync("ConfirmRideStatus", driverinfo, "Driver accepted your request.");

            // Fetch passenger details for return
            var passengerInfo = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserID == rejectMessagesDto.PassengerId)
                .Select(u => new
                {
                    UserID = u.UserID,
                    FullName = u.FullName,
                    SeatNumber = u.SeatNumber,
                    Gender = u.Gender,
                    PhoneNumber = u.PhoneNumber
                })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                message = "Ride Confirmed.",
                RideId = driverRide.RideID,
                availableSeats = driverRide.AvailableSeats,
                passengerinfo = passengerInfo,
                passengerRide = passengerRide,
            });
        }


        [HttpPost("RejectRide")]
        [Authorize]
        public async Task<IActionResult> RejectRide([FromQuery] int PassengerId)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest(new { message = "User ID not found in token." });
            }

            int userId = Convert.ToInt32(userIdClaim.Value);

            // Fetch the ride assigned to the driver
            var driverRide = await _context.Rides.FirstOrDefaultAsync(r => r.UserID == userId);
            var passengerRide = await _context.Rides.FirstOrDefaultAsync(r => r.UserID == PassengerId);

            // Check if a chat exists between the student and the driver
            var chat = await _context.Chats.Include(m => m.Messages).FirstOrDefaultAsync(c =>
                (c.ReceiverID == PassengerId && c.SenderID == Convert.ToInt16(userId)) ||
                (c.ReceiverID == Convert.ToInt16(userId) && c.SenderID == PassengerId)
            );

            if (driverRide == null || passengerRide == null || chat == null)
            {
                return NotFound(new { message = "Ride not found or unauthorized access." });
            }

            // Delete Chat
            _context.Chats.Remove(chat);
            await _context.SaveChangesAsync();

            // Send rejection message to the passenger
            await _hubContext.Clients.Group(PassengerId.ToString())
                .SendAsync("RejectRideStatus", new { UserID = userId }, "Driver rejected your ride request.");

            return Ok(new { message = "Ride request rejected." });
        }


        [HttpPost("CompleteRide")]
        [Authorize]
        public async Task<IActionResult> CompleteRide([FromBody] CompleteRide completeRide)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest(new { message = "User ID not found in token." });
            }

            var userId = Convert.ToInt32(userIdClaim.Value);

            // Fetch the ride assigned to the driver
            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.UserID == userId);
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
                                         where r.RideID == completeRide.RideId
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

            // Mark the passenger as unavailable
            var passenger = await _context.Rides.FirstOrDefaultAsync(p => p.UserID == completeRide.PassengerId);
            if (passenger != null)
            {
                passenger.IsAvailable = false;
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Send completion message to the passenger
            await _hubContext.Clients.Group(completeRide.PassengerId.ToString())
                .SendAsync("CompleteRideStatus", null, "Your ride has been completed.");

            return Ok(new
            {
                Message = "Ride marked as completed successfully.",
                RideID = completeRide.RideId,
            });
        }
    }
}
