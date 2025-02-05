using Azure.Core;
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
                (c.StudentID == messageDto.ReceiverID && c.DriverID == Convert.ToInt16(userId)) ||
                (c.StudentID == Convert.ToInt16(userId) && c.DriverID == messageDto.ReceiverID)
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
                    StudentID = messageDto.ReceiverID,
                    DriverID = Convert.ToInt16(userId),
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

            // Send the message to the receiver's group
            await _hubContext.Clients.Group(messageDto.ReceiverID.ToString())
                .SendAsync("ReceiveMessage", userId, messageDto.Message);

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
        [HttpGet("GetMessages")]
        public async Task<IActionResult> GetMessages([FromQuery] int chatId)
        {
            // Validate user ID from the token
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User ID not found in token." });
            }

            var userId = Convert.ToInt16(userIdClaim.Value);

            // Retrieve the chat and its messages
            var chat = await _context.Chats
                .Include(c => c.Messages).AsNoTracking()
                .FirstOrDefaultAsync(c => c.ChatID == chatId);

            if (chat == null)
            {
                return NotFound(new { message = "Chat not found." });
            }

            // Ensure the user is part of the chat
            if (chat.StudentID != userId && chat.DriverID != userId)
            {
                return Unauthorized(new { message = "You are not authorized to view this chat." });
            }

            // Prepare a response in a user-friendly format
            var response = new
            {
                ChatID = chat.ChatID,
                Participants = new
                {
                    StudentID = chat.StudentID,
                    DriverID = chat.DriverID
                },
                Messages = chat.Messages.OrderBy(m => m.SentOn).Select(m => new
                {
                    MessageID = m.MessageID,
                    SenderID = m.SenderID,
                    MessageContent = m.MessageContent,
                    SentOn = m.SentOn.ToString("yyyy-MM-dd HH:mm:ss")
                })
            };

            return Ok(response);
        }


        // Join a chat group (SignalR specific)
        [HttpPost("JoinChat")]
        public async Task<IActionResult> JoinChat([FromBody] JoinChatDto dto)
        {
            await _hubContext.Groups.AddToGroupAsync(dto.ConnectionId, dto.ChatId.ToString());
            return Ok(new { message = "Joined chat group." });
        }
    }
}
