using ChessDotNet;
using ChessOnline.Application.DTOs.Game;
using ChessOnline.Application.Interfaces;
using ChessOnline.Domain.Entities;
using ChessOnline.Domain.Enums;
using ChessOnline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;


namespace ChessOnline.Infrastructure.Services
{
    public class GameService : IGameService
    {
        private readonly AppDbContext _db;
        private readonly UserManager<User> _userManager;

        public GameService(AppDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ================== CREATE LOBBY ==================
        public async Task<LobbyDto> CreateLobbyAsync(string creatorUserId, CreateLobbyDto dto)
        {
            var creator = await _userManager.FindByIdAsync(creatorUserId);
            var lobby = new GameLobby
            {
                Name = string.IsNullOrWhiteSpace(dto.Name)
                    ? $"{creator?.NickName}'s Game"
                    : dto.Name,
                Player1Id = creatorUserId,
                IsPublic = dto.IsPublic,
                Password = dto.IsPublic ? null : dto.Password,
                InitialTimeSeconds = dto.InitialTimeSeconds,
                IncrementSeconds = dto.IncrementSeconds,
            };

            _db.GameLobbies.Add(lobby);
            await _db.SaveChangesAsync();

            return new LobbyDto
            {
                Id = lobby.Id,
                Name = lobby.Name,
                Player1Id = lobby.Player1Id,
                Player1Name = creator?.NickName,
                IsPublic = lobby.IsPublic,
                InitialTimeSeconds = lobby.InitialTimeSeconds,
                IncrementSeconds = lobby.IncrementSeconds,
                CreatedAt = lobby.CreatedAt
            };
        }

        // ================== GET AVAILABLE LOBBIES ==================
        public async Task<IEnumerable<LobbyDto>> GetAvailableLobbiesAsync()
        {
            return await _db.GameLobbies
                .Where(l => l.Player2Id == null)
                .OrderByDescending(x => x.CreatedAt)
                .Select(l => new LobbyDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    Player1Id = l.Player1Id,
                    Player1Name = l.Player1 != null ? l.Player1.NickName : null,
                    Player2Id = l.Player2Id,
                    IsPublic = l.IsPublic,
                    InitialTimeSeconds = l.InitialTimeSeconds,
                    IncrementSeconds = l.IncrementSeconds,
                    CreatedAt = l.CreatedAt
                })
                .Take(100)
                .ToListAsync();
        }

        // ================== JOIN LOBBY ==================
        public async Task<(bool Success, Guid? LobbyId, string Message)> JoinLobbyAsync(string userId, JoinLobbyDto dto)
        {
            var lobby = await _db.GameLobbies.FirstOrDefaultAsync(g => g.Id == dto.LobbyId);
            if (lobby == null)
                return (false, null, "Phòng không tồn tại.");

            if (!lobby.IsPublic)
            {
                if (string.IsNullOrEmpty(dto.Password) || lobby.Password != dto.Password)
                    return (false, null, "Sai mật khẩu hoặc thiếu mật khẩu.");
            }

            if (!string.IsNullOrEmpty(lobby.Player1Id) && !string.IsNullOrEmpty(lobby.Player2Id))
                return (false, null, "Phòng đã đầy.");

            if (lobby.Player1Id == userId || lobby.Player2Id == userId)
                return (true, lobby.Id, "Bạn đã ở trong phòng này.");

            if (string.IsNullOrEmpty(lobby.Player1Id))
                lobby.Player1Id = userId;
            else
                lobby.Player2Id = userId;

            _db.GameLobbies.Update(lobby);
            await _db.SaveChangesAsync();

            return (true, lobby.Id, "Tham gia phòng thành công.");
        }

        // ================== LEAVE LOBBY ==================
        public async Task LeaveLobbyAsync(string userId, Guid lobbyId)
        {
            var lobby = await _db.GameLobbies.FindAsync(lobbyId);
            if (lobby == null) return;

            var gamePlay = await _db.GamePlays.FirstOrDefaultAsync(g => g.LobbyId == lobbyId);
            if (gamePlay != null) return;

            if (lobby.Player1Id == userId) lobby.Player1Id = null;
            if (lobby.Player2Id == userId) lobby.Player2Id = null;

            if (lobby.Player1Id == null && lobby.Player2Id == null)
                _db.GameLobbies.Remove(lobby);
            else
                _db.GameLobbies.Update(lobby);

            await _db.SaveChangesAsync();
        }

        // ================== START GAME ==================
        public async Task<GamePlay?> StartGameAsync(Guid lobbyId)
        {
            var lobby = await _db.GameLobbies
                .Include(l => l.Player1)
                .Include(l => l.Player2)
                .FirstOrDefaultAsync(l => l.Id == lobbyId);

            if (lobby?.Player1Id == null || lobby.Player2Id == null)
                return null;

            var players = new[] { lobby.Player1, lobby.Player2 };
            var random = new Random();
            var whitePlayer = players[random.Next(players.Length)];
            var blackPlayer = players.First(p => p != whitePlayer);

            var newGamePlay = new GamePlay
            {
                LobbyId = lobbyId,
                WhitePlayerId = whitePlayer.Id,
                BlackPlayerId = blackPlayer.Id,
                // ⭐️ THAY ĐỔI: Sử dụng FEN mặc định của ChessDotNet
                CurrentFen = new ChessGame().GetFen(),
                Result = GameResult.Pending,
                WhiteRemainingSeconds = lobby.InitialTimeSeconds,
                BlackRemainingSeconds = lobby.InitialTimeSeconds
            };

            await _db.GamePlays.AddAsync(newGamePlay);
            await _db.SaveChangesAsync();

            return newGamePlay;
        }

        // ================== MAKE MOVE ==================
        public async Task<(bool Success, string? NewFen, bool IsGameOver, string Message)>
        MakeMoveAsync(Guid lobbyId, string from, string to, string userId)
        {
            var gamePlay = await _db.GamePlays.FirstOrDefaultAsync(gp => gp.LobbyId == lobbyId);
            if (gamePlay == null)
                return (false, null, false, "Không tìm thấy ván cờ.");

            // ✅ FEN mặc định nếu chưa có
            var fen = string.IsNullOrEmpty(gamePlay.CurrentFen) || gamePlay.CurrentFen == "startpos"
                ? new ChessGame().GetFen()
                : gamePlay.CurrentFen;

            var game = new ChessGame(fen);

            // ✅ Xác định người chơi
            var playerColor = gamePlay.WhitePlayerId == userId ? Player.White : Player.Black;

            // ✅ Kiểm tra lượt
            if (game.WhoseTurn != playerColor)
                return (false, null, false, "Chưa đến lượt của bạn.");

            // ✅ Chuẩn hóa nước đi
            string fromLower = from.ToLower();
            string toLower = to.ToLower();
            char? promotionPiece = null;

            if (toLower.Length == 3)
                promotionPiece = toLower[2];
            else if (from.Length == 4)
            {
                fromLower = from.Substring(0, 2);
                toLower = from.Substring(2, 2);
            }

            var move = new Move(fromLower, toLower, playerColor, promotionPiece ?? '\0');

            if (!game.IsValidMove(move))
                return (false, null, false, "Nước đi không hợp lệ.");

            var moveResult = game.MakeMove(move, true);

            if (moveResult == MoveType.Invalid)
                return (false, null, false, "Không thể thực hiện nước đi.");

            // ✅ Cập nhật FEN
            var newFen = game.GetFen();
            gamePlay.CurrentFen = newFen;

            // ✅ Lưu lịch sử
            var movesList = new List<string>();
            try
            {
                if (!string.IsNullOrEmpty(gamePlay.MoveHistoryJson))
                    movesList = JsonSerializer.Deserialize<List<string>>(gamePlay.MoveHistoryJson) ?? new List<string>();
            }
            catch { }

            movesList.Add($"{fromLower}-{toLower}");
            gamePlay.MoveHistoryJson = JsonSerializer.Serialize(movesList);

            // ✅ Kiểm tra trạng thái game
            bool isGameOver = false;
            string? lobbyResultStr = null;

            // ⚙️ Kiểm tra lần lượt các trạng thái hợp lệ
            if (game.IsCheckmated(Player.White))
            {
                isGameOver = true;
                gamePlay.Result = GameResult.BlackWins; // Đen chiếu hết Trắng
                lobbyResultStr = "0-1";
            }
            else if (game.IsCheckmated(Player.Black))
            {
                isGameOver = true;
                gamePlay.Result = GameResult.WhiteWins; // Trắng chiếu hết Đen
                lobbyResultStr = "1-0";
            }
            else if (game.IsStalemated(Player.White) || game.IsStalemated(Player.Black))
            {
                isGameOver = true;
                gamePlay.Result = GameResult.DrawByStalemate;
                lobbyResultStr = "1/2-1/2";
            }
            else if (game.IsDraw())
            {
                isGameOver = true;
                gamePlay.Result = GameResult.Draw;
                lobbyResultStr = "1/2-1/2";
            }
            else if (game.IsInsufficientMaterial())
            {
                isGameOver = true;
                gamePlay.Result = GameResult.DrawByInsufficientMaterial;
                lobbyResultStr = "1/2-1/2";
            }

            if (isGameOver)
                gamePlay.EndTime = DateTime.UtcNow;

            // ✅ Cập nhật GameLobby
            var lobby = await _db.GameLobbies.FirstOrDefaultAsync(l => l.Id == lobbyId);
            if (lobby != null)
            {
                lobby.CurrentFen = newFen;
                lobby.LastMove = $"{fromLower}-{toLower}";
                lobby.Turn = newFen.Contains(" w ") ? "w" : "b";
                lobby.IsGameOver = isGameOver;
                if (isGameOver && lobbyResultStr != null)
                    lobby.Result = lobbyResultStr;
                _db.GameLobbies.Update(lobby);
            }

            _db.GamePlays.Update(gamePlay);
            await _db.SaveChangesAsync();

            return (true, newFen, isGameOver, isGameOver ? "Game finished" : "Thực hiện nước đi thành công.");
        }

        // ================== GET GAMEPLAY ==================
        // (Không thay đổi)
        public async Task<GamePlay?> GetGamePlayByLobbyIdAsync(Guid lobbyId)
        {
            return await _db.GamePlays.FirstOrDefaultAsync(gp => gp.LobbyId == lobbyId);
        }

        // ⭐️ THAY ĐỔI: Xóa phương thức TryParseMove của logic cũ
        // private static bool TryParseMove(string from, string to, out Move move) { ... }

        // ================== GET LOBBY BY ID ==================
        // (Không thay đổi)
        public async Task<GameLobby?> GetLobbyByIdAsync(Guid lobbyId)
        {
            return await _db.GameLobbies
                .Include(l => l.Player1)
                .Include(l => l.Player2)
                .FirstOrDefaultAsync(l => l.Id == lobbyId);
        }

        // ================== TRY UPDATE BOARD STATE ==================
        // (Không thay đổi)
        public async Task<bool> TryUpdateBoardStateAsync(
            Guid lobbyId,
            string fen,
            string move)
        {
            var lobby = await _db.GameLobbies.FirstOrDefaultAsync(l => l.Id == lobbyId);
            if (lobby == null)
                return false;

            lobby.CurrentFen = fen;
            lobby.LastMove = move;
            lobby.Turn = fen.Contains(" w ") ? "w" : "b";
            lobby.IsGameOver = false;
            lobby.Result = null;

            _db.GameLobbies.Update(lobby);
            await _db.SaveChangesAsync();
            return true;
        }

    }
}