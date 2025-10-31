using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOnline.Application.DTOs.Game
{
    public class LobbyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Player1Id { get; set; }
        public string? Player1Name { get; set; }
        public string? Player2Id { get; set; }
        public string? Player2Name { get; set; }
        public bool IsPublic { get; set; }
        public bool IsFull => Player1Id != null && Player2Id != null;
        public int InitialTimeSeconds { get; set; }
        public int IncrementSeconds { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
