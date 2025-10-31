using System;
using System.ComponentModel.DataAnnotations;

namespace ChessOnline.Domain.Entities
{
    public class GameLobby
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string? Name { get; set; }

        public string? Player1Id { get; set; }
        public virtual User? Player1 { get; set; }

        public string? Player2Id { get; set; }
        public virtual User? Player2 { get; set; }

        public bool IsPublic { get; set; } = true;

        [MaxLength(50)]
        public string? Password { get; set; }

        public int InitialTimeSeconds { get; set; } = 300;
        public int IncrementSeconds { get; set; } = 3;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🔹 Bổ sung trạng thái ván đấu
        public string CurrentFen { get; set; } = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        public string? LastMove { get; set; }
        public string Turn { get; set; } = "w";
        public bool IsGameOver { get; set; } = false;
        public string? Result { get; set; } // Ví dụ: "1-0", "0-1", "1/2-1/2"
    }
}
