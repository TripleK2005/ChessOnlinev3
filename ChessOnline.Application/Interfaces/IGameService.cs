using ChessOnline.Application.DTOs.Game;
using ChessOnline.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOnline.Application.Interfaces
{
    public interface IGameService
    {
        Task<LobbyDto> CreateLobbyAsync(string creatorUserId, CreateLobbyDto dto);
        Task<IEnumerable<LobbyDto>> GetAvailableLobbiesAsync();
        Task<(bool Success, Guid? LobbyId, string Message)> JoinLobbyAsync(string userId, JoinLobbyDto dto);
        Task LeaveLobbyAsync(string userId, Guid lobbyId);
        Task<GamePlay?> StartGameAsync(Guid lobbyId);
        Task<(bool Success, string? NewFen, bool IsGameOver, string Message)> MakeMoveAsync(Guid lobbyId, string from, string to, string userId);
        Task<GamePlay?> GetGamePlayByLobbyIdAsync(Guid lobbyId);
        Task<GameLobby?> GetLobbyByIdAsync(Guid lobbyId);
        Task<bool> TryUpdateBoardStateAsync(Guid lobbyId, string fen, string move);
        Task<bool> UpdateClocksAsync(Guid lobbyId, int whiteRemainingSeconds, int blackRemainingSeconds);
    }
}
