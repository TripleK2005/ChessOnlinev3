using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChessOnline.Domain.Entities;

namespace ChessOnline.Infrastructure.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // chuyển enum sang string
            builder.Property(u => u.Status)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(u => u.NickName)
                .IsRequired()
                .HasMaxLength(50);

            // index nếu muốn
            builder.HasIndex(u => u.NickName).IsUnique();
        }
    }
}
