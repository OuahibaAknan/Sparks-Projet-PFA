using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sparks.Api.Models;

namespace Sparks.Api.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(255);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(255);
        builder.Property(u => u.IdStellantis).HasMaxLength(255);
        builder.Property(u => u.AvatarColor).HasMaxLength(50);

        builder.HasOne(u => u.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(u => u.TeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(255);
        builder.Property(t => t.Department).HasMaxLength(255);
        builder.Property(t => t.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Token).IsRequired().HasMaxLength(255);
        builder.Property(r => r.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
