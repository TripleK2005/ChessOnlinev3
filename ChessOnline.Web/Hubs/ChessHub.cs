using Microsoft.AspNetCore.SignalR;

namespace ChessOnline.Web.Hubs
{
    public class ChessHub : Hub
    {
        public Task JoinLobby(string lobbyId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
        }

        public Task LeaveLobby(string lobbyId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId);
        }

        public async Task SendMessage(string lobbyId, string user, string message)
        {
            await Clients.Group(lobbyId).SendAsync("ReceiveMessage", user, message);
        }
    }
}
