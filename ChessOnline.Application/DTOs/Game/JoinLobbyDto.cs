using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOnline.Application.DTOs.Game
{
    public class JoinLobbyDto
    {
        public Guid LobbyId { get; set; }
        public string? Password { get; set; }
    }
}
