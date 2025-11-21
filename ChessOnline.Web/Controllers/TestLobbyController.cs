using Azure.Core;
using ChessOnline.Application.DTOs.Game;
using ChessOnline.Application.Interfaces;
using ChessOnline.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;

namespace ChessOnline.Web.Controllers
{
    [Authorize]
    [Route("TestLobby")]
    public class TestLobbyController : Controller
    {
        private readonly IGameService _gameService;
        private readonly UserManager<User> _userManager;

        public TestLobbyController(IGameService gameService, UserManager<User> userManager)
        {
            _gameService = gameService;
            _userManager = userManager;
        }
        public ActionResult Index()
        {
            return View();
        }

        // POST /TestLobby/Create
        // Body: CreateLobbyDto
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreateLobbyDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var lobby = await _gameService.CreateLobbyAsync(userId, dto);
            var playUrl = Url.Action("Play", "Match", new { lobbyId = lobby.Id }, Request.Scheme) ?? $"/Game/Play/{lobby.Id}";

            return Ok(new
            {
                success = true,
                lobbyId = lobby.Id,
                name = lobby.Name,
                playUrl
            });
        }

        // POST /TestLobby/Join
        // Body: { "lobbyId": "...", "password": "..." }
        public class JoinRequest
        {
            public Guid LobbyId { get; set; }
            public string? Password { get; set; }
        }

        [HttpPost("Join")]
        public async Task<IActionResult> Join([FromBody] JoinRequest req)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var joinDto = new JoinLobbyDto
            {
                LobbyId = req.LobbyId,
                Password = req.Password
            };

            var (success, lobbyId, message) = await _gameService.JoinLobbyAsync(userId, joinDto);
            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new
            {
                success = true,
                lobbyId,
                message
            });
        }

        // GET /TestLobby/List
        // Simple helper for debugging: returns up to 100 latest lobbies
        [HttpGet("List")]
        public async Task<IActionResult> List()
        {
            var lobbies = await _gameService.GetAvailableLobbiesAsync();
            return Ok(lobbies);
        }
    }
}