using ChessOnline.Application.Interfaces;
using ChessOnline.Domain.Entities;
using ChessOnline.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChessOnline.Web.Controllers
{
    [Authorize]
    public class GameController : ControllerBase
    {
        private readonly IGameService _gameService;
        private readonly UserManager<User> _userManager;

        public GameController(IGameService gameService, UserManager<User> userManager)
        {
            _gameService = gameService;
            _userManager = userManager;
        }

        public class MoveDto
        {
            public Guid LobbyId { get; set; }
            public string From { get; set; } = "";
            public string To { get; set; } = "";

            // optional clock sync sent by client when making a move
            public int? WhiteRemainingSeconds { get; set; }
            public int? BlackRemainingSeconds { get; set; }
        }

        public class ClockSyncDto
        {
            public Guid LobbyId { get; set; }
            public int WhiteRemainingSeconds { get; set; }
            public int BlackRemainingSeconds { get; set; }
        }

        [HttpPost("sync-clock")]
        public async Task<IActionResult> SyncClock([FromBody] ClockSyncDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var ok = await _gameService.UpdateClocksAsync(dto.LobbyId, dto.WhiteRemainingSeconds, dto.BlackRemainingSeconds);
            if (!ok) return NotFound();
            return Ok(new { success = true });
        }

        [HttpPost("move")]
        public async Task<IActionResult> MakeMove([FromBody] MoveDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var (success, newFen, isGameOver, message) = await _gameService.MakeMoveAsync(dto.LobbyId, dto.From, dto.To, userId);

            if (!success)
                return BadRequest(new { success = false, message });

            // apply clock update if client provided values
            if (dto.WhiteRemainingSeconds.HasValue && dto.BlackRemainingSeconds.HasValue)
            {
                await _gameService.UpdateClocksAsync(dto.LobbyId, dto.WhiteRemainingSeconds.Value, dto.BlackRemainingSeconds.Value);
            }

            return Ok(new { success = true, fen = newFen, isGameOver });
        }

        [HttpGet("state/{lobbyId:guid}")]
        public async Task<IActionResult> GetState(Guid lobbyId)
        {
            var gp = await _gameService.GetGamePlayByLobbyIdAsync(lobbyId);
            if (gp == null)
            {
                var lobby = await _gameService.GetLobbyByIdAsync(lobbyId);
                if (lobby == null) return NotFound();
                return Ok(new
                {
                    fen = lobby.CurrentFen,
                    lastMove = lobby.LastMove,
                    turn = lobby.Turn,
                    isGameOver = lobby.IsGameOver,
                    whiteRemainingSeconds = (int?)null,
                    blackRemainingSeconds = (int?)null,
                    whitePlayerId = lobby.Player1Id,
                    blackPlayerId = lobby.Player2Id
                });
            }

            return Ok(new
            {
                fen = gp.CurrentFen,
                moveHistory = gp.MoveHistoryJson,
                isGameOver = gp.Result != null && gp.Result.ToString() != "Pending",
                whiteRemainingSeconds = gp.WhiteRemainingSeconds,
                blackRemainingSeconds = gp.BlackRemainingSeconds,
                whitePlayerId = gp.WhitePlayerId,
                blackPlayerId = gp.BlackPlayerId
            });
        }
    }
}