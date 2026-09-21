using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sparks.Api.Models;

namespace Sparks.Api.Data.Configurations;

public class KnowledgeBaseEntryConfiguration : IEntityTypeConfiguration<KnowledgeBaseEntry>
{
    public void Configure(EntityTypeBuilder<KnowledgeBaseEntry> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Title).IsRequired().HasMaxLength(255);
        builder.Property(k => k.Content).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(k => k.Tags).HasMaxLength(1000);
        builder.Property(k => k.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(k => k.Domain)
            .WithMany()
            .HasForeignKey(k => k.DomainId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(s => s.Domain)
            .WithMany()
            .HasForeignKey(s => s.DomainId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Type).IsRequired().HasMaxLength(255);
        builder.Property(n => n.Message).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(n => n.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Ticket)
            .WithMany()
            .HasForeignKey(n => n.TicketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(255);
        builder.Property(a => a.Entity).IsRequired().HasMaxLength(255);
        builder.Property(a => a.TimestampUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
