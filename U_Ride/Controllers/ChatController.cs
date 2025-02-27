﻿using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(ApplicationDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        //===========================================================================================//

        // <summary>
        // Send message to all
        // </summary>
        // <param name="message"></param>

        /*

        [HttpPost("{message}")]
        public void Post(string message)
        {
            _hubContext.Clients.All.SendAsync("publicMessageMethodName", message);
        }

        */

        // <summary>
        // Send message to specific client
        // </summary>
        // <param name="connectionId"></param>
        // <param name="message"></param>

        /*
         
        [HttpPost("SendPrivateMessage")]
        public void Post(SendMessageDto sendMessageDto)
        {
            _hubContext.Clients.Client(sendMessageDto.ConnectionID).SendAsync("privateMessageMethodName", sendMessageDto.Message);
        }

        */

        //============================================================================================//

        /*

        // APPROACH # 1: CLIENT ID TO SEND PRIVATE MESSAGE

        [HttpPost("SendPrivateMessage")]
        [Authorize]
        public async Task<IActionResult> SendPrivateMessage(SendMessageDto sendMessageDto)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest(new { message = "User ID not found in token" });
            }

            var userId = userIdClaim.Value;
            // Check if the user is connected
            if (ChatHub.UserConnections.TryGetValue((sendMessageDto.ReceiverID).ToString(), out var connectionId))
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("privateMessageMethodName", sendMessageDto.Message);
                return Ok("Message sent successfully.");
            }

            return NotFound(new { message = "User not connected." });
        }

        */

        //============================================================================================//

        // APPROACH # 2: USE PRIVATE GROUP TO SEND PRIVATE MESSAGES

        [HttpPost("SendPersonalMessage")]
        [Authorize]
        public async Task<IActionResult> SendPersonalMessage([FromBody] SendMessageDto messageDto)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest(new { message = "User ID not found in token." });
            }

            var userId = userIdClaim.Value;

            // Check if a chat exists between the student and the driver
            var chat = await _context.Chats.Include(m => m.Messages).FirstOrDefaultAsync(c =>
                (c.ReceiverID == messageDto.ReceiverID && c.SenderID == Convert.ToInt16(userId)) ||
                (c.ReceiverID == Convert.ToInt16(userId) && c.SenderID == messageDto.ReceiverID)
            );

            if (chat != null)
            {
                // Add a new message to the existing chat
                chat.Messages?.Add(new Message
                {
                    SenderID = Convert.ToInt16(userId),
                    MessageContent = messageDto.Message,
                    SentOn = DateTime.UtcNow
                });
            }
            else
            {
                var userInfo = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.UserID == Convert.ToInt16(userId))
                    .Select(u => new UserInfo
                    {
                        UserID = u.UserID,
                        FullName = u.FullName,
                        Gender = u.Gender,
                        PhoneNumber = u.PhoneNumber,
                        RideInfo = new RideInfo
                        {
                            StartPoint = u.Ride.StartPoint,
                            EndPoint = u.Ride.EndPoint,
                            EncodedPolyline = u.Ride.EncodedPolyline
                        },
                        VehicleInfo = u.Vehicle == null ? null : new VehicleInfo
                        {
                            VehicleType = u.Vehicle.VehicleType,
                            Make_Model = u.Vehicle.Make + " " + u.Vehicle.Model,
                            Color = u.Vehicle.Color,
                            LicensePlate = u.Vehicle.LicensePlate
                        }
                    })
                    .FirstOrDefaultAsync();

                if (userInfo == null)
                {
                    return NotFound(new { message = "Ride information not found." });
                }

                // Create a new chat
                chat = new Chat
                {
                    ReceiverID = messageDto.ReceiverID,
                    SenderID = Convert.ToInt16(userId),
                    StartedOn = DateTime.UtcNow,
                    Messages = new List<Message>
                    {
                        new Message
                        {
                            SenderID = Convert.ToInt16(userId),
                            MessageContent = messageDto.Message,
                            SentOn = DateTime.UtcNow
                        }
                    }
                };
                _context.Chats.Add(chat);

                // Save changes to the database
                await _context.SaveChangesAsync();

                // List of participants (sender and receiver)
                var participants = new List<(int userId, object userInfo)>
                {
                    (messageDto.ReceiverID, userInfo),  // Receiver gets sender's info
                    (Convert.ToInt16(userId), null)      // Sender gets no info
                };

                foreach (var (participantId, info) in participants)
                {
                    // Send the message to each participant
                    await _hubContext.Clients.Group(participantId.ToString())
                        .SendAsync("IntroMessage", new { ChatID = chat.ChatID, UserInfo = info });
                }

            }

            var lastMessage = chat.Messages.Last();

            var messageInfoDto = new MessageInfoDto
            {
                MessageID = lastMessage.MessageID,
                SenderID = Convert.ToInt32(userId),
                MessageContent = messageDto.Message,
                SentOn = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // Send the message to the receiver's group
            await _hubContext.Clients.Group(messageDto.ReceiverID.ToString())
                .SendAsync("ReceiveMessage", messageInfoDto);

            // Save changes (if a new message was added to an existing chat)
            await _context.SaveChangesAsync();

            return Ok(new { message = "Message sent successfully." });
        }

        //============================================================================================//

        // Get messages of a chat
        [Authorize]
        [HttpGet("StartChat")]
        public async Task<IActionResult> StartChat([FromQuery] int ReceiverId)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User ID not found in token." });
            }

            int userId = int.Parse(userIdClaim.Value);

            if (userId == ReceiverId)
            {
                return BadRequest(new { message = "You cannot start a chat with yourself." });
            }

            // Check if a chat already exists between sender and receiver
            var chat = await _context.Chats
                .Include(c => c.Messages)
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    (c.SenderID == userId && c.ReceiverID == ReceiverId) ||
                    (c.SenderID == ReceiverId && c.ReceiverID == userId));

            // If chat does not exist, create a new one
            if (chat == null)
            {
                chat = new Chat
                {
                    ReceiverID = ReceiverId,
                    SenderID = userId,
                    StartedOn = DateTime.UtcNow
                };
                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();
            }

            // Fetch receiver's ride info
            var receiverRide = await _context.Rides
                .Where(r => r.UserID == ReceiverId)
                .Select(r => new RideDto.RideInfo
                {
                    RideID = r.RideID,
                    StartPoint = r.StartPoint,
                    EndPoint = r.EndPoint,
                    EncodedPolyline = r.EncodedPolyline,
                    AvailableSeats = r.AvailableSeats.ToString()
                }).AsNoTracking()
                .FirstOrDefaultAsync();

            // Fetch receiver's info
            var userinfo = await _context.Users
                .Where(u => u.UserID == ReceiverId)
                .Select(u => new RideDto.UserInfo
                {
                    UserID = u.UserID,
                    FullName = u.FullName,
                    Gender = u.Gender,
                    PhoneNumber = u.PhoneNumber,
                    RideInfo = receiverRide
                }).AsNoTracking()
                .FirstOrDefaultAsync();

            var response = new
            {
                ChatID = chat.ChatID,
                Participants = new
                {
                    SenderID = chat.SenderID,
                    ReceiverID = chat.ReceiverID
                },
                UserInfo = userinfo,
                Messages = chat.Messages
                    .OrderBy(m => m.SentOn)
                    .Select(m => new
                    {
                        MessageID = m.MessageID,
                        SenderID = m.SenderID,
                        MessageContent = m.MessageContent,
                        SentOn = m.SentOn.ToString("yyyy-MM-dd HH:mm:ss")
                    })
            };

            return Ok(response);
        }


        [Authorize]
        [HttpPost("GetMessages")]
        public async Task<IActionResult> GetMessages([FromBody] GetMessagesDto getMessagesDto)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest(new { message = "User ID not found in token." });
            }

            var userId = Convert.ToInt32(userIdClaim.Value);

            if (getMessagesDto.ChatIDs == null || !getMessagesDto.ChatIDs.Any())
            {
                return BadRequest(new { message = "Chat ID list cannot be empty." });
            }

            // Get the current user's HasVehicle status
            var currentUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (currentUser == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var chatMessages = new List<ChatInfoDto>();

            foreach (var chatId in getMessagesDto.ChatIDs)
            {
                var chat = await _context.Chats.AsNoTracking()
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.ChatID == chatId);

                if (chat == null) continue; // Skip if chat not found

                // Get opponent user
                var opponentId = chat.ReceiverID == userId ? chat.SenderID : chat.ReceiverID;
                var opponent = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserID == opponentId);

                // Skip chat if both users have the same HasVehicle value (either true or false)
                if (opponent != null && currentUser.HasVehicle == opponent.HasVehicle)
                {
                    continue;
                }

                var lastMessage = chat.Messages
                    .OrderByDescending(m => m.SentOn)
                    .FirstOrDefault();

                if (lastMessage == null) continue; // Skip if no messages

                AuthDto.VehicleInfo? vehicleInfo = null;
                if (opponent?.HasVehicle == true)
                {
                    var vehicle = await _context.Vehicles.AsNoTracking()
                        .FirstOrDefaultAsync(v => v.UserID == opponent.UserID);

                    if (vehicle != null)
                    {
                        vehicleInfo = new AuthDto.VehicleInfo
                        {
                            VehicleType = vehicle.VehicleType,
                            Make_Model = $"{vehicle.Make} {vehicle.Model}",
                            LicensePlate = vehicle.LicensePlate
                        };
                    }
                }

                chatMessages.Add(new ChatInfoDto
                {
                    ChatID = chat.ChatID,
                    UserInfo = opponent == null ? null : new AuthDto.UserInfo
                    {
                        UserID = opponent.UserID,
                        FullName = opponent.FullName,
                        SeatNumber = opponent.SeatNumber,
                        Gender = opponent.Gender,
                        PhoneNumber = opponent.PhoneNumber,
                        Vehicle = vehicleInfo
                    },
                    MessageInfo = new MessageInfoDto
                    {
                        MessageID = lastMessage.MessageID,
                        SenderID = lastMessage.SenderID,
                        MessageContent = lastMessage.MessageContent,
                        SentOn = lastMessage.SentOn.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                });
            }

            if (!chatMessages.Any())
            {
                return NotFound(new { message = "No messages found for the provided chat IDs." });
            }

            return Ok(chatMessages);
        }

    }
}
