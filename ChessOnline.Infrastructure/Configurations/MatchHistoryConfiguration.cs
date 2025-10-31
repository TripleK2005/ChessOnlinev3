using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChessOnline.Domain.Entities;
using ChessOnline.Domain.Enums;

namespace ChessOnline.Infrastructure.Configurations
{
    public class MatchHistoryConfiguration : IEntityTypeConfiguration<MatchHistory>
    {
        public void Configure(EntityTypeBuilder<MatchHistory> builder)
        {
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Moves)
                .IsRequired();

            builder.Property(m => m.Result)
                .HasConversion<string>()
                .IsRequired();

            builder.HasOne(m => m.WhitePlayer)
                .WithMany(u => u.WhiteMatches)
                .HasForeignKey(m => m.WhitePlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.BlackPlayer)
                .WithMany(u => u.BlackMatches)
                .HasForeignKey(m => m.BlackPlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
