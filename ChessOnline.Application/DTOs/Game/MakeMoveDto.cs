using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOnline.Application.DTOs.Game
{
    public class MakeMoveDto
    {
        public Guid LobbyId { get; set; }
        public string From { get; set; } = string.Empty; // e2
        public string To { get; set; } = string.Empty;   // e4
        public string? Promotion { get; set; } // e.g. "q"
    }
}
