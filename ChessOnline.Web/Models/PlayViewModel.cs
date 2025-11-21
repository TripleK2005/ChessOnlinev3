using System;

namespace ChessOnline.Web.Models
{
    public class PlayViewModel
    {
        public Guid LobbyId { get; set; }
        public string? CurrentUserId { get; set; }
    }
}