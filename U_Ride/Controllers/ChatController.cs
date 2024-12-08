using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using U_Ride.DTOs;
using U_Ride.Models;
using U_Ride.Services;

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

        [HttpPost("SendPrivateMessage")]
        [Authorize]
        public async Task<IActionResult> Post(SendMessageDto sendMessageDto)
        {
            var userIdClaim = User.FindFirst("UserID");
            if (userIdClaim == null)
            {
                return BadRequest("User ID not found in token");
            }

            var userId = userIdClaim.Value;
            // Check if the user is connected
            if (ChatHub.UserConnections.TryGetValue((sendMessageDto.ReceiverID).ToString(), out var connectionId))
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("privateMessageMethodName", sendMessageDto.Message);
                return Ok("Message sent successfully.");
            }

            return NotFound("User not connected.");
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
        [HttpGet("GetMessages/{chatId}")]
        public async Task<IActionResult> GetMessages(int chatId)
        {
            var messages = await _context.Messages
                .Where(m => m.ChatID == chatId)
                .OrderBy(m => m.SentOn)
                .ToListAsync();

            if (!messages.Any())
                return NotFound("No messages found for this chat.");

            return Ok(messages);
        }

        // Join a chat group (SignalR specific)
        [HttpPost("JoinChat")]
        public async Task<IActionResult> JoinChat([FromBody] JoinChatDto dto)
        {
            await _hubContext.Groups.AddToGroupAsync(dto.ConnectionId, dto.ChatId.ToString());
            return Ok("Joined chat group.");
        }
    }
}
