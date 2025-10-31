using ChessOnline.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChessOnline.Domain.Entities
{
    public class User : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        public string? NickName { get; set; }

        public string? AvatarUrl { get; set; } = "/uploads/avatars/default-avatar.png";

        public AccountStatus Status { get; set; } = AccountStatus.Active;

        // Thống kê & xếp hạng
        public int EloRating { get; set; } = 1200;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public int Draws { get; set; } = 0;

        public double WinRate
        {
           get
           {
               int played = Wins + Losses;
               if (played == 0) return 0;
               return (double)Wins / played;
           }
        }

        // Navigation properties
        public virtual ICollection<MatchHistory> WhiteMatches { get; set; } = new List<MatchHistory>();
        public virtual ICollection<MatchHistory> BlackMatches { get; set; } = new List<MatchHistory>();
        public virtual ICollection<Friendship> SentFriendRequests { get; set; } = new List<Friendship>();
        public virtual ICollection<Friendship> ReceivedFriendRequests { get; set; } = new List<Friendship>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<Chat> SentChats { get; set; } = new List<Chat>();
        public virtual ICollection<Chat> ReceivedChats { get; set; } = new List<Chat>();
        public virtual ICollection<Report> ReportsMade { get; set; } = new List<Report>();
        public virtual ICollection<Report> ReportsReceived { get; set; } = new List<Report>();

        // Behavior / logic nghiệp vụ
        public void UpdateStatsAfterMatch(GameResult result, bool isWhitePlayer)
        {
            switch (result)
            {
                case GameResult.WhiteWins:
                    if (isWhitePlayer) Wins++;
                    else Losses++;
                    break;

                case GameResult.BlackWins:
                    if (!isWhitePlayer) Wins++;
                    else Losses++;
                    break;

                // Nếu bạn muốn gom các loại hòa vào một case:
                case GameResult.DrawByStalemate:
                case GameResult.DrawByRepetition:
                case GameResult.DrawByAgreement:
                case GameResult.DrawByInsufficientMaterial:
                    Draws++;
                    break;

                case GameResult.Aborted:
                    // Có thể bạn không tính cái gì khi trận bị huỷ
                    break;
            }
        }

        public double GetWinRate()
        {
            int played = Wins + Losses;
            if (played == 0) return 0;
            return (double)Wins / played;
        }
    }
}
