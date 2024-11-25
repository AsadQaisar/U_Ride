using Microsoft.AspNetCore.SignalR;
using U_Ride.Data;
using U_Ride.Models;

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

        public void GetDataFromClient(string userId, string connectionId)
        {
            Clients.Client(connectionId).SendAsync("clientMethodName", $"Updated userid {userId}");
        }

        public override Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            Clients.Client(connectionId).SendAsync("WelcomeMethodName", connectionId);
            return base.OnConnectedAsync();
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
