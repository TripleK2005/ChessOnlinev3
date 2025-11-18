using ChessOnline.Domain.Entities;
using ChessOnline.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChessOnline.Web.Controllers
{
    [Authorize]
    [Route("Game")]
    public class MatchController : Controller
    {
        private readonly UserManager<User> _userManager;

        public MatchController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        // URL: /Game/Play/{lobbyId}
        [HttpGet("Play/{lobbyId:guid}")]
        public IActionResult Play(Guid lobbyId)
        {
            var userId = _userManager.GetUserId(User);
            var model = new PlayViewModel
            {
                LobbyId = lobbyId,
                CurrentUserId = userId
            };
            // explicit view path to keep using Views/Game/Play.cshtml
            return View("~/Views/Game/Play.cshtml", model);
        }
    }
}