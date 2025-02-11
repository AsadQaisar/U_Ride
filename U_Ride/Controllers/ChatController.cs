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

        [HttpPost("{message}")]
        public void Post(string message)
        {
            _hubContext.Clients.All.SendAsync("publicMessageMethodName", message);
        }

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

        /*
        // Start a new chat
        [HttpPost("StartChat")]
        [Authorize]
        public async Task<IActionResult> StartChat([FromBody] StartChatDto dto)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest("User ID not found in token");
            }

            var userId = Convert.ToInt32(userIdClaim.Value);

            // Check if the driver exists
            var driver = await _context.Users.FirstOrDefaultAsync(u => u.UserID == dto.DriverId && u.HasVehicle == true);
            if (driver == null)
                return BadRequest("Driver not found or is not a driver.");

            var chats = await _context.Chats.FirstOrDefaultAsync(c => c.StudentID == userId && c.DriverID == dto.DriverId);
            if (chats != null)
            {
                chats.StartedOn = DateTime.UtcNow;
            }
            else
            {
                // Create a new chat entry
                var chat = new Chat
                {
                    StudentID = userId,
                    DriverID = dto.DriverId,
                    StartedOn = DateTime.UtcNow
                };
                await _context.Chats.AddAsync(chat);
            }

            await _context.SaveChangesAsync();

            // Notify the driver about the new chat (via SignalR)
            await _hubContext.Clients.User(dto.DriverId.ToString()).SendAsync("NewChatStarted", chats.ChatID);

            return Ok(new { chats.ChatID });
        }

        // Send a message
        [HttpPost("SendMessage")]
        [Authorize]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest("User ID not found in token");
            }

            var userId = Convert.ToInt32(userIdClaim.Value);

            var chat = await _context.Chats.FirstOrDefaultAsync(c => c.ChatID == dto.ChatId);
            if (chat == null)
                return NotFound("Chat not found.");

            // Save the message to the database
            var message = new Message
            {
                ChatID = dto.ChatId,
                SenderID = userId,
                MessageContent = dto.Message,
                SentOn = DateTime.UtcNow
            };

            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();

            // Broadcast the message to the chat participants
            await _hubContext.Clients.Group(dto.ChatId.ToString())
                .SendAsync("ReceiveMessage", userId, dto.Message, message.SentOn);

            return Ok(message);
        }
        */

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
                return NotFound(new { message = "Chat not found." });
            }

            var response = new
            {
                ChatID = chat.ChatID,
                Participants = new
                {
                    SenderID = chat.SenderID,
                    ReceiverID = chat.ReceiverID
                },
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


        // Get all chats of the user
        [Authorize]
        [HttpPost("GetMessages")]
        public async Task<IActionResult> GetMessages([FromBody] GetMessagesDto getMessagesDto)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest(new { message = "User ID not found in token." });
            }

            var userId = userIdClaim.Value;

            if (getMessagesDto.ChatIDs == null || !getMessagesDto.ChatIDs.Any())
            {
                return BadRequest(new { message = "Chat ID list cannot be empty." });
            }

            var chatMessages = new List<ChatInfoDto>();

            foreach (var chatId in getMessagesDto.ChatIDs)
            {
                var chat = await _context.Chats.AsNoTracking()
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.ChatID == chatId);

                if (chat == null) continue; // Skip if chat not found

                var lastMessage = chat.Messages
                    .OrderByDescending(m => m.SentOn)
                    .FirstOrDefault();

                if (lastMessage == null) continue; // Skip if no messages

                var receiver = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserID == (chat.ReceiverID == Convert.ToInt32(userId) ? chat.SenderID : chat.ReceiverID));


                AuthDto.VehicleInfo? vehicleInfo = null;
                if (receiver.HasVehicle == true)
                {
                    var vehicle = await _context.Vehicles.AsNoTracking()
                        .FirstOrDefaultAsync(v => v.UserID == receiver.UserID);

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
                    UserInfo = receiver == null ? null : new AuthDto.UserInfo
                    {
                        UserID = receiver.UserID,
                        FullName = receiver.FullName,
                        SeatNumber = receiver.SeatNumber,
                        Gender = receiver.Gender,
                        PhoneNumber = receiver.PhoneNumber,
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
