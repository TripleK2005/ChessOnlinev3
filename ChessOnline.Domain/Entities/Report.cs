using System;
using System.ComponentModel.DataAnnotations;

namespace ChessOnline.Domain.Entities
{
    public class Report
    {
        public Guid Id { get; set; }

        [Required]
        public string? ReporterId { get; set; }
        public virtual User? Reporter { get; set; }
        public string? AdminId { get; set; }
        public virtual User? Admin { get; set; }

        [Required]
        public string? ReportedUserId { get; set; }
        public virtual User? ReportedUser { get; set; }

        [Required]
        [MaxLength(500)]
        public string? Reason { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool IsResolved { get; set; } = false;
        public DateTime? ResolvedAt { get; set; }

        public void Resolve()
        {
            if (!IsResolved)
            {
                IsResolved = true;
                ResolvedAt = DateTime.UtcNow;
            }
        }
    }
}
