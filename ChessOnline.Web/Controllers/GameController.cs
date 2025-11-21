using ChessOnline.Application.Interfaces;
using ChessOnline.Application.DTOs.Game;
using ChessOnline.Domain.Entities;
using ChessOnline.Domain.Enums;
using ChessOnline.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChessOnline.Web.Controllers
{
    [Authorize]
    public class GameController : Controller
    {
        private readonly IGameService _gameService;
        private readonly UserManager<User> _userManager;

        public GameController(IGameService gameService, UserManager<User> userManager)
        {
            _gameService = gameService;
            _userManager = userManager;
        }

        public IActionResult Lobby()
        {
            return View();
        }

        [HttpGet("Play/{lobbyId}")]
        public async Task<IActionResult> Play(Guid lobbyId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var lobby = await _gameService.GetLobbyByIdAsync(lobbyId);
            if (lobby == null) return NotFound("Lobby not found");

            ViewBag.LobbyId = lobbyId;
            ViewBag.UserId = userId;
            return View();
        }

        [HttpPost("create-lobby")]
        public async Task<IActionResult> CreateLobby([FromBody] CreateLobbyDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var lobby = await _gameService.CreateLobbyAsync(userId, dto);
            return Ok(new { success = true, lobbyId = lobby.Id });
        }

        [HttpPost("join-lobby")]
        public async Task<IActionResult> JoinLobby([FromBody] JoinLobbyDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var (success, lobbyId, message) = await _gameService.JoinLobbyAsync(userId, dto);
            if (!success) return BadRequest(new { success = false, message });

            return Ok(new { success = true, lobbyId });
        }

        [HttpGet("list-lobbies")]
        public async Task<IActionResult> ListLobbies()
        {
            var lobbies = await _gameService.GetAvailableLobbiesAsync();
            return Ok(lobbies);
        }

        public class MoveDto
        {
            public Guid LobbyId { get; set; }
            public string From { get; set; } = "";
            public string To { get; set; } = "";
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
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var ok = await _gameService.UpdateClocksAsync(dto.LobbyId, dto.WhiteRemainingSeconds, dto.BlackRemainingSeconds);
            if (!ok) return NotFound();
            return Ok(new { success = true });
        }

        [HttpPost("move")]
        public async Task<IActionResult> MakeMove([FromBody] MoveDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var (success, newFen, isGameOver, message) = await _gameService.MakeMoveAsync(dto.LobbyId, dto.From, dto.To, userId);

            if (!success) return BadRequest(new { success = false, message });

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
                    whitePlayerId = lobby.Player1Id,
                    blackPlayerId = lobby.Player2Id,
                    whitePlayerName = lobby.Player1?.NickName ?? "Unknown",
                    blackPlayerName = lobby.Player2?.NickName ?? "Unknown",
                    whiteRemainingSeconds = lobby.InitialTimeSeconds,
                    blackRemainingSeconds = lobby.InitialTimeSeconds,
                    isGameOver = lobby.IsGameOver
                });
            }

            // Need to fetch names for GamePlay too, but GamePlay might not have navigation properties populated by GetGamePlayByLobbyIdAsync
            // Let's fetch the lobby to get names if needed, or rely on GameService to include them.
            // Checking GameService.GetGamePlayByLobbyIdAsync... it does NOT include players.
            // So we need to fetch lobby or users.
            var lobbyForNames = await _gameService.GetLobbyByIdAsync(lobbyId);

            return Ok(new
            {
                fen = gp.CurrentFen,
                whitePlayerId = gp.WhitePlayerId,
                blackPlayerId = gp.BlackPlayerId,
                whitePlayerName = lobbyForNames?.Player1?.NickName ?? "Unknown",
                blackPlayerName = lobbyForNames?.Player2?.NickName ?? "Unknown",
                whiteRemainingSeconds = gp.WhiteRemainingSeconds,
                blackRemainingSeconds = gp.BlackRemainingSeconds,
                isGameOver = gp.Result != GameResult.Pending
            });
        }
    }
}