using System;
using ChessOnline.Domain.Enums;

namespace ChessOnline.Domain.Entities
{
    public class Friendship
    {
        public Guid Id { get; set; }

        public string? RequesterId { get; set; }
        public virtual User? Requester { get; set; }

        public string? AddresseeId { get; set; }
        public virtual User? Addressee { get; set; }

        public FriendshipStatus Status { get; set; }
        public DateTime RequestTime { get; set; }
    }
}
