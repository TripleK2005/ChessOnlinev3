using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOnline.Application.DTOs.Game
{
    public class BoardStateDto
    {
        public string FEN { get; set; } = string.Empty;
        public string LastMove { get; set; } = string.Empty; // e2e4
        public string Turn { get; set; } = "w"; // "w" or "b"
        public bool IsGameOver { get; set; } = false;
        public string? Result { get; set; } // "1-0","0-1","1/2-1/2"
    }
}
