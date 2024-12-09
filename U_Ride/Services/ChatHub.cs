using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using U_Ride.Data;
using U_Ride.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace U_Ride.Services
{
    public class ChatHub : Hub
    {
        private readonly SharedDb _shared;

        public ChatHub(SharedDb shared)
        {
            _shared = shared;
        }

        public async Task JoinChat(UserConnection conn)
        {
            await Clients.All.SendAsync("ReceiveMessage", "admin", $"{conn.Username} has joined.");
        }

        public async Task JoinSpecificChatRoom(UserConnection conn)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conn.ChatRoom);
            _shared.connections[Context.ConnectionId] = conn;
            await Clients.Group(conn.ChatRoom).SendAsync("ReceiveMessage", "admin", $"{conn.Username} has joined {conn.ChatRoom}.");
        }

        public async Task SendMessage(string msg)
        {
            if (_shared.connections.TryGetValue(Context.ConnectionId, out UserConnection conn))
            {
                await Clients.Group(conn.ChatRoom).SendAsync("ReceiveSpecificMessage", conn.Username, msg);
            }
        }

        //============================================================================================//

        // APPROACH # 1: CLIENT ID TO SEND PRIVATE MESSAGE

        public static Dictionary<string, string> UserConnections = new Dictionary<string, string>();

        public void GetDataFromClient(string userId, string connectionId)
        {
            Clients.Client(connectionId).SendAsync("clientMethodName", $"Updated userid {userId}");
        }

        /*
        [Authorize]
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            var token = Context.GetHttpContext().Request.Query["access_token"];

            if (token != StringValues.Empty)
            {
                try
                {
                    // Decode the token to extract the user ID
                    var userId = ExtractUserIdFromToken(token);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        // Store the connection ID for the user
                        UserConnections[userId] = connectionId;

                        // Send a welcome message to the client
                        await Clients.Client(connectionId).SendAsync("WelcomeMethodName", connectionId, userId);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error decoding token: {ex.Message}");
                }
            }

            await base.OnConnectedAsync();
        }
        
        public override Task OnDisconnectedAsync(Exception exception)
        {
            var connectionId = Context.ConnectionId;
            return base.OnDisconnectedAsync(exception);
        }
        */

        //=============================================================================================//

        // APPROACH # 2: USE PRIVATE GROUP TO SEND PRIVATE MESSAGES

        public override async Task OnConnectedAsync()
        {
            var token = Context.GetHttpContext().Request.Query["access_token"];
            var userId = ExtractUserIdFromToken(token);

            if (!string.IsNullOrEmpty(userId))
            {
                await Clients.Client(Context.ConnectionId).SendAsync("WelcomeMethodName", Context.ConnectionId, userId);
            }

            await base.OnConnectedAsync();
        }

        // Add the user to the group based on their user ID
        public async Task JoinGroup()
        {
            var token = Context.GetHttpContext()?.Request.Query["access_token"];

            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Access token is missing.");
            }

            var userId = ExtractUserIdFromToken(token);

            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("User ID is invalid or missing from token.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        // Remove user from the group when they disconnect
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        private string ExtractUserIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken?.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
        }

        //============================================================================================//

        /*
        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync("ReceiveMessage", $"{Context.ConnectionId} has joined.");
        }
        
        public async Task SendMessage(string chatId, string userId, string message)
        {
            // Broadcast message to clients in the same chat
            await Clients.Group(chatId).SendAsync("ReceiveMessage", userId, message);
        }

        public async Task JoinChat(string chatId)
        {
            // Add the user to a SignalR group for the chat
            await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
        }
        */
        public async Task LeaveChat(string chatId)
        {
            // Remove the user from the SignalR group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);
        }
    }
}
