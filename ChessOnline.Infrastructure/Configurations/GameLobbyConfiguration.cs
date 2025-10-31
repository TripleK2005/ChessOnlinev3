using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChessOnline.Domain.Entities;

namespace ChessOnline.Infrastructure.Configurations
{
    public class GameLobbyConfiguration : IEntityTypeConfiguration<GameLobby>
    {
        public void Configure(EntityTypeBuilder<GameLobby> builder)
        {
            builder.HasKey(gl => gl.Id);

            builder.Property(gl => gl.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(gl => gl.Password)
                .HasMaxLength(50);

            builder.HasOne(gl => gl.Player1)
                .WithMany()
                .HasForeignKey(gl => gl.Player1Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(gl => gl.Player2)
                .WithMany()
                .HasForeignKey(gl => gl.Player2Id)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
