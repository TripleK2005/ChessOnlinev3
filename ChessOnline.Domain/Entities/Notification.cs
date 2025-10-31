using System;
using System.ComponentModel.DataAnnotations;
using ChessOnline.Domain.Enums;

namespace ChessOnline.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }

        [Required]
        public string? RecipientId { get; set; }
        public virtual User? Recipient { get; set; }

        public string? SenderId { get; set; }
        public virtual User? Sender { get; set; }

        public NotificationType Type { get; set; }

        [Required]
        [MaxLength(255)]
        public string? Content { get; set; }

        [MaxLength(500)]
        public string? LinkUrl { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public void MarkAsRead()
        {
            IsRead = true;
        }
    }
}
