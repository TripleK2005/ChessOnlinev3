using ChessOnline.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOnline.Domain.Entities
{
    public class GamePlay
    {
        [Key]
        public Guid Id { get; set; }

        public Guid LobbyId { get; set; }
        public virtual GameLobby? Lobby { get; set; }

        [Required]
        public string? WhitePlayerId { get; set; }
        public virtual User? WhitePlayer { get; set; }

        [Required]
        public string? BlackPlayerId { get; set; }
        public virtual User? BlackPlayer { get; set; }

        public GameResult Result { get; set; } = GameResult.Pending;

        public string CurrentFen { get; set; } = "startpos"; // vị trí bàn cờ hiện tại
        public string MoveHistoryJson { get; set; } = "[]";  // danh sách nước đi

        public int WhiteRemainingSeconds { get; set; }
        public int BlackRemainingSeconds { get; set; }

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
    }
}
