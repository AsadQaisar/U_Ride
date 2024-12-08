using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;
using System.IdentityModel.Tokens.Jwt;
using U_Ride.Data;
using U_Ride.Models;

namespace U_Ride.Services
{
    public class ChatHub : Hub
    {
        private readonly SharedDb _shared;

        public static Dictionary<string, string> UserConnections = new Dictionary<string, string>();

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

        public void GetDataFromClient(string userId, string connectionId)
        {
            Clients.Client(connectionId).SendAsync("clientMethodName", $"Updated userid {userId}");
        }

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

        private string ExtractUserIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken?.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var connectionId = Context.ConnectionId;
            return base.OnDisconnectedAsync(exception);
        }

        //=============================================================================================//

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
