using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChessDotNet;
using ChessOnline.Application.DTOs.Game;
using ChessOnline.Infrastructure.Persistence;
using ChessOnline.Infrastructure.Services;
using ChessOnline.Domain.Entities;
using ChessOnline.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ChessOnline.Test.Services
{
    public class GameServiceTests
    {
        private static AppDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        private static Mock<UserManager<User>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        [Fact]
        public async Task CreateLobbyAsync_DefaultNameAndPrivatePasswordStored()
        {
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateInMemoryContext(dbName);
            var user = new User { Id = "u1", NickName = "Tester" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var um = CreateMockUserManager();
            um.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);

            var svc = new GameService(ctx, um.Object);

            var dto = new CreateLobbyDto
            {
                Name = "", // should use default nickname-based name
                IsPublic = false,
                Password = "p@ss",
                InitialTimeSeconds = 300,
                IncrementSeconds = 5
            };

            var result = await svc.CreateLobbyAsync("u1", dto);

            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Contains("Tester's Game", result.Name);
            Assert.Equal("u1", result.Player1Id);
            Assert.False(result.IsPublic);
            Assert.Equal(300, result.InitialTimeSeconds);
            Assert.Equal(5, result.IncrementSeconds);

            // verify stored in DB and password not exposed by dto (dto doesn't include password)
            var stored = await ctx.GameLobbies.FindAsync(result.Id);
            Assert.NotNull(stored);
            Assert.Equal("p@ss", stored.Password);
        }

        [Fact]
        public async Task GetAvailableLobbiesAsync_ReturnsOnlyThoseWithNoPlayer2()
        {
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateInMemoryContext(dbName);

            var u = new User { Id = "u1", NickName = "A" };
            ctx.Users.Add(u);
            var l1 = new GameLobby { Id = Guid.NewGuid(), Name = "L1", Player1Id = "u1", Player2Id = null, CreatedAt = DateTime.UtcNow.AddMinutes(-1) };
            var l2 = new GameLobby { Id = Guid.NewGuid(), Name = "L2", Player1Id = "u1", Player2Id = "p2", CreatedAt = DateTime.UtcNow };
            ctx.GameLobbies.AddRange(l1, l2);
            await ctx.SaveChangesAsync();

            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            var available = (await svc.GetAvailableLobbiesAsync()).ToList();

            Assert.Single(available);
            Assert.Equal(l1.Id, available[0].Id);
        }

        [Fact]
        public async Task JoinLobbyAsync_LobbyNotFound_ReturnsError()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            var (success, lobbyId, message) = await svc.JoinLobbyAsync("u", new JoinLobbyDto { LobbyId = Guid.NewGuid() });

            Assert.False(success);
            Assert.Null(lobbyId);
            Assert.Equal("Phòng không tồn tại.", message);
        }

        [Fact]
        public async Task JoinLobbyAsync_PrivateWrongPassword_ReturnsError()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            // Name is required by the entity configuration — set it in tests
            var lobby = new GameLobby { Id = Guid.NewGuid(), Name = "PrivateLobby", IsPublic = false, Password = "correct" };
            ctx.GameLobbies.Add(lobby);
            await ctx.SaveChangesAsync();

            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            var (success, lobbyId, message) = await svc.JoinLobbyAsync("u2", new JoinLobbyDto { LobbyId = lobby.Id, Password = "bad" });

            Assert.False(success);
            Assert.Null(lobbyId);
            Assert.Equal("Sai mật khẩu hoặc thiếu mật khẩu.", message);
        }

        [Fact]
        public async Task JoinLobbyAsync_JoinAsPlayer2_Succeeds()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var lobby = new GameLobby { Id = Guid.NewGuid(), Name = "OpenLobby", IsPublic = true, Player1Id = "p1", Player2Id = null };
            ctx.GameLobbies.Add(lobby);
            await ctx.SaveChangesAsync();

            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            var (success, lobbyId, message) = await svc.JoinLobbyAsync("p2", new JoinLobbyDto { LobbyId = lobby.Id });

            Assert.True(success);
            Assert.Equal(lobby.Id, lobbyId);
            Assert.Equal("Tham gia phòng thành công.", message);

            var stored = await ctx.GameLobbies.FindAsync(lobby.Id);
            Assert.Equal("p2", stored.Player2Id);
        }

        [Fact]
        public async Task JoinLobbyAsync_UserAlreadyIn_ReturnsAlreadyIn()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var lobby = new GameLobby { Id = Guid.NewGuid(), Name = "SameUserLobby", IsPublic = true, Player1Id = "same", Player2Id = null };
            ctx.GameLobbies.Add(lobby);
            await ctx.SaveChangesAsync();

            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            var (success, lobbyId, message) = await svc.JoinLobbyAsync("same", new JoinLobbyDto { LobbyId = lobby.Id });

            Assert.True(success);
            Assert.Equal(lobby.Id, lobbyId);
            Assert.Equal("Bạn đã ở trong phòng này.", message);
        }

        [Fact]
        public async Task LeaveLobbyAsync_RemovesLobbyWhenBothLeave()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var lobby = new GameLobby { Id = Guid.NewGuid(), Name = "TwoPlayerLobby", Player1Id = "a", Player2Id = "b" };
            ctx.GameLobbies.Add(lobby);
            await ctx.SaveChangesAsync();

            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            // first user a leaves -> player2 remains
            await svc.LeaveLobbyAsync("a", lobby.Id);
            var stored = await ctx.GameLobbies.FindAsync(lobby.Id);
            Assert.NotNull(stored);
            Assert.Null(stored.Player1Id);
            Assert.Equal("b", stored.Player2Id);

            // then b leaves -> lobby removed
            await svc.LeaveLobbyAsync("b", lobby.Id);
            stored = await ctx.GameLobbies.FindAsync(lobby.Id);
            Assert.Null(stored);
        }

        [Fact]
        public async Task LeaveLobbyAsync_DoesNothingIfGamePlayExists()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var lobby = new GameLobby { Id = Guid.NewGuid(), Name = "BusyLobby", Player1Id = "a", Player2Id = null };
            // GamePlay requires WhitePlayerId, BlackPlayerId and CurrentFen — set them
            var gp = new GamePlay { Id = Guid.NewGuid(), LobbyId = lobby.Id, WhitePlayerId = "a", BlackPlayerId = "x", CurrentFen = new ChessGame().GetFen(), MoveHistoryJson = "[]" };
            ctx.GameLobbies.Add(lobby);
            ctx.GamePlays.Add(gp);
            await ctx.SaveChangesAsync();

            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            await svc.LeaveLobbyAsync("a", lobby.Id);

            var stored = await ctx.GameLobbies.FindAsync(lobby.Id);
            Assert.NotNull(stored);
            Assert.Equal("a", stored.Player1Id); // unchanged because a cannot leave while a gameplay exists
        }

        [Fact]
        public async Task StartGameAsync_ReturnsNullIfNotFull()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var lobby = new GameLobby { Id = Guid.NewGuid(), Name = "WaitingLobby", Player1Id = "p1", Player2Id = null, InitialTimeSeconds = 60 };
            ctx.GameLobbies.Add(lobby);
            await ctx.SaveChangesAsync();

            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            var gp = await svc.StartGameAsync(lobby.Id);
            Assert.Null(gp);
        }

        [Fact]
        public async Task StartGameAsync_CreatesGamePlay_WhenBothPlayersPresent()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var p1 = new User { Id = "p1", NickName = "One" };
            var p2 = new User { Id = "p2", NickName = "Two" };
            ctx.Users.AddRange(p1, p2);
            var lobby = new GameLobby
            {
                Id = Guid.NewGuid(),
                Name = "ReadyLobby",
                Player1Id = p1.Id,
                Player2Id = p2.Id,
                Player1 = p1,
                Player2 = p2,
                InitialTimeSeconds = 120
            };
            ctx.GameLobbies.Add(lobby);
            await ctx.SaveChangesAsync();

            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            var gp = await svc.StartGameAsync(lobby.Id);
            Assert.NotNull(gp);
            Assert.Equal(lobby.Id, gp.LobbyId);
            Assert.Contains(" w ", gp.CurrentFen); // starting fen contains " w "
            Assert.Equal(GameResult.Pending, gp.Result);
            Assert.Equal(120, gp.WhiteRemainingSeconds);
            Assert.Equal(120, gp.BlackRemainingSeconds);

            // persisted
            var stored = await ctx.GamePlays.FirstOrDefaultAsync(g => g.LobbyId == lobby.Id);
            Assert.NotNull(stored);
        }

        [Fact]
        public async Task MakeMoveAsync_ReturnsError_WhenGamePlayMissing()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            var res = await svc.MakeMoveAsync(Guid.NewGuid(), "e2", "e4", "u");
            Assert.False(res.Success);
            Assert.Equal("Không tìm thấy ván cờ.", res.Message);
        }

        [Fact]
        public async Task MakeMoveAsync_RejectsWhenNotPlayersTurn()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            // create gameplay where white is player "w", but call with other user
            var gp = new GamePlay { Id = Guid.NewGuid(), LobbyId = Guid.NewGuid(), WhitePlayerId = "w", BlackPlayerId = "b", CurrentFen = new ChessGame().GetFen(), MoveHistoryJson = "[]" };
            ctx.GamePlays.Add(gp);
            await ctx.SaveChangesAsync();

            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            var res = await svc.MakeMoveAsync(gp.LobbyId, "e2", "e4", "b"); // b is black, but starting turn is white, so black trying => error
            Assert.False(res.Success);
            Assert.Equal("Chưa đến lượt của bạn.", res.Message);
        }

        [Fact]
        public async Task MakeMoveAsync_PerformsValidMove_UpdatesFenAndHistory()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var lobbyId = Guid.NewGuid();
            var gp = new GamePlay
            {
                Id = Guid.NewGuid(),
                LobbyId = lobbyId,
                WhitePlayerId = "w",
                BlackPlayerId = "b",
                CurrentFen = new ChessGame().GetFen(),
                MoveHistoryJson = "[]"
            };
            var lobby = new GameLobby { Id = lobbyId, Name = "PlayingLobby", CurrentFen = gp.CurrentFen, Player1Id = "w", Player2Id = "b" };
            ctx.GamePlays.Add(gp);
            ctx.GameLobbies.Add(lobby);
            await ctx.SaveChangesAsync();

            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            var (success, newFen, isGameOver, message) = await svc.MakeMoveAsync(lobbyId, "e2", "e4", "w");

            Assert.True(success);
            Assert.False(isGameOver);
            Assert.Equal("Thực hiện nước đi thành công.", message);
            Assert.NotNull(newFen);

            var storedGp = await ctx.GamePlays.FirstAsync(g => g.LobbyId == lobbyId);
            var moves = JsonSerializer.Deserialize<List<string>>(storedGp.MoveHistoryJson) ?? new List<string>();
            Assert.Contains("e2-e4", moves);
            Assert.Equal(newFen, storedGp.CurrentFen);

            var storedLobby = await ctx.GameLobbies.FindAsync(lobbyId);
            Assert.Equal(storedGp.CurrentFen, storedLobby.CurrentFen);
            Assert.Equal("e2-e4", storedLobby.LastMove);
            Assert.True(storedLobby.Turn == "w" || storedLobby.Turn == "b");
        }

        [Fact]
        public async Task MakeMoveAsync_InvalidMove_ReturnsInvalidMessage()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var lobbyId = Guid.NewGuid();
            var gp = new GamePlay
            {
                Id = Guid.NewGuid(),
                LobbyId = lobbyId,
                WhitePlayerId = "w",
                BlackPlayerId = "b",
                CurrentFen = new ChessGame().GetFen(),
                MoveHistoryJson = "[]"
            };
            ctx.GamePlays.Add(gp);
            await ctx.SaveChangesAsync();

            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            // e2 to e5 is not valid in one move
            var res = await svc.MakeMoveAsync(lobbyId, "e2", "e5", "w");
            Assert.False(res.Success);
            Assert.Equal("Nước đi không hợp lệ.", res.Message);
        }

        [Fact]
        public async Task GetGamePlayByLobbyIdAsync_ReturnsGameplay()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var lobbyId = Guid.NewGuid();
            // GamePlay has required properties — set them
            var gp = new GamePlay { Id = Guid.NewGuid(), LobbyId = lobbyId, WhitePlayerId = "w", BlackPlayerId = "b", CurrentFen = new ChessGame().GetFen(), MoveHistoryJson = "[]" };
            ctx.GamePlays.Add(gp);
            await ctx.SaveChangesAsync();

            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            var fetched = await svc.GetGamePlayByLobbyIdAsync(lobbyId);
            Assert.NotNull(fetched);
            Assert.Equal(gp.Id, fetched.Id);
        }

        [Fact]
        public async Task GetLobbyByIdAsync_IncludesPlayers()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var p1 = new User { Id = "p1", NickName = "One" };
            var p2 = new User { Id = "p2", NickName = "Two" };
            ctx.Users.AddRange(p1, p2);
            var lobby = new GameLobby { Id = Guid.NewGuid(), Name = "WithPlayers", Player1Id = p1.Id, Player2Id = p2.Id, Player1 = p1, Player2 = p2 };
            ctx.GameLobbies.Add(lobby);
            await ctx.SaveChangesAsync();

            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            var fetched = await svc.GetLobbyByIdAsync(lobby.Id);
            Assert.NotNull(fetched);
            Assert.NotNull(fetched.Player1);
            Assert.NotNull(fetched.Player2);
            Assert.Equal("One", fetched.Player1.NickName);
            Assert.Equal("Two", fetched.Player2.NickName);
        }

        [Fact]
        public async Task TryUpdateBoardStateAsync_ReturnsFalseWhenLobbyMissing_TrueWhenUpdated()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var um = CreateMockUserManager();
            var svc = new GameService(ctx, um.Object);

            var notFound = await svc.TryUpdateBoardStateAsync(Guid.NewGuid(), "fen", "e2-e4");
            Assert.False(notFound);

            var lobbyId = Guid.NewGuid();
            var lobby = new GameLobby { Id = lobbyId, Name = "UpdatableLobby", CurrentFen = "start", Turn = "w", IsGameOver = true, Result = "1-0" };
            ctx.GameLobbies.Add(lobby);
            await ctx.SaveChangesAsync();

            var ok = await svc.TryUpdateBoardStateAsync(lobbyId, "newfen w KQkq - 0 1", "e2-e4");
            Assert.True(ok);

            var stored = await ctx.GameLobbies.FindAsync(lobbyId);
            Assert.Equal("newfen w KQkq - 0 1", stored.CurrentFen);
            Assert.Equal("e2-e4", stored.LastMove);
            Assert.Equal("w", stored.Turn);
            Assert.False(stored.IsGameOver);
            Assert.Null(stored.Result);
        }
    }
}