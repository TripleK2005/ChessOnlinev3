using ChessOnline.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOnline.Infrastructure.Configurations
{
    public class GamePlayConfiguration : IEntityTypeConfiguration<GamePlay>
    {
        public void Configure(EntityTypeBuilder<GamePlay> builder)
        {
            // --- Khóa chính ---
            builder.HasKey(gp => gp.Id);

            // --- Cấu hình các thuộc tính ---
            builder.Property(gp => gp.CurrentFen)
                .IsRequired();

            // Lưu trữ Enum dưới dạng chuỗi (string) để dễ đọc trong database
            builder.Property(gp => gp.Result)
                .HasConversion<string>()
                .IsRequired();

            // --- Cấu hình các mối quan hệ (Relationships) ---

            // Mối quan hệ 1-1 với GameLobby
            // Nếu Lobby bị xóa, GamePlay tương ứng cũng sẽ bị xóa.
            builder.HasOne(gp => gp.Lobby)
                .WithOne() // Một GameLobby chỉ có một GamePlay
                .HasForeignKey<GamePlay>(gp => gp.LobbyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Mối quan hệ với người chơi Trắng
            // Không cho phép xóa User nếu họ đang trong một trận đấu.
            builder.HasOne(gp => gp.WhitePlayer)
                .WithMany() // Một User có thể chơi nhiều trận
                .HasForeignKey(gp => gp.WhitePlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mối quan hệ với người chơi Đen
            builder.HasOne(gp => gp.BlackPlayer)
                .WithMany()
                .HasForeignKey(gp => gp.BlackPlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
