using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sparks.Api.Models;

namespace Sparks.Api.Data.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.ToTable("Applications");
        builder.HasKey(a => a.ApplicationId);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(255);
        builder.Property(a => a.Description).HasColumnType("nvarchar(max)");
    }
}

public class UserApplicationConfiguration : IEntityTypeConfiguration<UserApplication>
{
    public void Configure(EntityTypeBuilder<UserApplication> builder)
    {
        builder.ToTable("UserApplication");
        builder.HasKey(ua => new { ua.ApplicationId, ua.UserId });

        builder.HasOne(ua => ua.Application)
            .WithMany()
            .HasForeignKey(ua => ua.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ua => ua.User)
            .WithMany()
            .HasForeignKey(ua => ua.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
