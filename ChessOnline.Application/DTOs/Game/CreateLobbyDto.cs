using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOnline.Application.DTOs.Game
{
    public class CreateLobbyDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = true;
        public string? Password { get; set; }
        public int InitialTimeSeconds { get; set; } = 300;
        public int IncrementSeconds { get; set; } = 3;
    }
}
