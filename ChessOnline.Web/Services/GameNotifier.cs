using ChessOnline.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using ChessOnline.Web.Hubs;
using System;
using System.Threading.Tasks;

namespace ChessOnline.Web.Services
{
    public class GameNotifier : IGameNotifier
    {
        private readonly IHubContext<ChessHub> _hub;

        public GameNotifier(IHubContext<ChessHub> hub)
        {
            _hub = hub;
        }

        public Task NotifyMoveAsync(Guid lobbyId, string fen, string move, bool isGameOver, int whiteRemainingSeconds, int blackRemainingSeconds)
        {
            // Broadcast to the group named by lobbyId
            return _hub.Clients.Group(lobbyId.ToString())
                .SendAsync("ReceiveMove", lobbyId.ToString(), fen, move, isGameOver, whiteRemainingSeconds, blackRemainingSeconds);
        }
    }
}