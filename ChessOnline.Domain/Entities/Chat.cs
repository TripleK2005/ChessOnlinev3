using System;
using System.ComponentModel.DataAnnotations;

namespace ChessOnline.Domain.Entities
{
    public class Chat
    {
        public Guid Id { get; set; }

        [Required]
        public string? SenderId { get; set; }
        public virtual User? Sender { get; set; }

        [Required]
        public string? ReceiverId { get; set; }
        public virtual User? Receiver { get; set; }

        [Required]
        public string? Message { get; set; }

        public DateTime Timestamp { get; set; }

        public bool IsDeleted { get; set; } = false;
        public bool IsEdited { get; set; } = false;
    }
}
