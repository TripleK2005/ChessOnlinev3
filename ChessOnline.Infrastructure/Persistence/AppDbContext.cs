using ChessOnline.Domain.Entities;
using ChessOnline.Infrastructure.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace ChessOnline.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<MatchHistory> MatchHistories { get; set; }
        public DbSet<GameLobby> GameLobbies { get; set; }
        public DbSet<GamePlay> GamePlays { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Report> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // sẽ thêm cấu hình mapping ở đây hoặc dùng ApplyConfiguration
            builder.ApplyConfiguration(new UserConfiguration());
            builder.ApplyConfiguration(new MatchHistoryConfiguration());
            builder.ApplyConfiguration(new GameLobbyConfiguration());
            builder.ApplyConfiguration(new GamePlayConfiguration());
            builder.ApplyConfiguration(new FriendshipConfiguration());
            builder.ApplyConfiguration(new NotificationConfiguration());
            builder.ApplyConfiguration(new ChatConfiguration());
            builder.ApplyConfiguration(new ReportConfiguration());
        }
    }
}
