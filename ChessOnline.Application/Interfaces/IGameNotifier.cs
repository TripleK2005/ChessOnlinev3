using System;
using System.Threading.Tasks;

namespace ChessOnline.Application.Interfaces
{
    public interface IGameNotifier
    {
        Task NotifyMoveAsync(Guid lobbyId, string fen, string move, bool isGameOver, int whiteRemainingSeconds, int blackRemainingSeconds);
    }
}
    