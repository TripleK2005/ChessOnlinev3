using System;
using System.ComponentModel.DataAnnotations;
using ChessOnline.Domain.Enums;

namespace ChessOnline.Domain.Entities
{
    public class MatchHistory
    {
        public Guid Id { get; set; }

        [Required]
        public string? WhitePlayerId { get; set; }
        public virtual User? WhitePlayer { get; set; }

        [Required]
        public string? BlackPlayerId { get; set; } 
        public virtual User? BlackPlayer { get; set; }

        [Required]
        public GameResult Result { get; set; }

        [Required]
        public string? Moves { get; set; }

        public int InitialTimeSeconds { get; set; }
        public int IncrementSeconds { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
