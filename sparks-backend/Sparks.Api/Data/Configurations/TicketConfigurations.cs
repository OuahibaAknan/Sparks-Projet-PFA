using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sparks.Api.Models;

namespace Sparks.Api.Data.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(t => t.Id);
        // Column renamed (not the C# property, to avoid a cascading rename across the whole app) to
        // match the external/legacy "Tickets" table exactly for raw-SQL/reporting compatibility.
        builder.Property(t => t.Id).HasColumnName("TicketId");

        builder.Property(t => t.ReferenceKZ).IsRequired().HasMaxLength(255);
        builder.HasIndex(t => t.ReferenceKZ).IsUnique();
        builder.Property(t => t.ReferenceSTLA).HasMaxLength(255);

        builder.Property(t => t.ApplicantId).HasMaxLength(255);
        builder.Property(t => t.ApplicantFirstName).HasMaxLength(255);
        builder.Property(t => t.ApplicantLastName).HasMaxLength(255);

        builder.Property(t => t.Application).IsRequired().HasMaxLength(255);
        builder.Property(t => t.SubModule).HasMaxLength(255);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(255);
        builder.Property(t => t.Description).HasColumnType("nvarchar(max)");
        builder.Property(t => t.Summary).HasColumnName("TicketSummary").HasColumnType("nvarchar(max)");
        builder.Property(t => t.EnglishTicketSummary).HasColumnType("nvarchar(max)");
        builder.Property(t => t.EnglishAccepted).HasColumnName("IsEnglishAccepted");
        builder.Property(t => t.Priority).HasColumnName("Gravity");
        builder.Property(t => t.JiraLink).HasMaxLength(255);
        builder.Property(t => t.PartNumber).HasMaxLength(255);
        builder.Property(t => t.EcrNumber).HasMaxLength(255);

        builder.Property(t => t.Category).HasMaxLength(255);
        builder.Property(t => t.Organization).HasMaxLength(255);
        builder.Property(t => t.Origin).HasMaxLength(255);
        builder.Property(t => t.StudyCell).HasMaxLength(255);

        builder.Property(t => t.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.SourceSystemId);

        builder.HasOne(t => t.SourceSystem)
            .WithMany()
            .HasForeignKey(t => t.SourceSystemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Module)
            .WithMany()
            .HasForeignKey(t => t.ModuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Domain)
            .WithMany()
            .HasForeignKey(t => t.DomainId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Site)
            .WithMany()
            .HasForeignKey(t => t.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Assignee)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class TicketAttachmentConfiguration : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.FileName).IsRequired().HasMaxLength(255);
        builder.Property(a => a.Url).IsRequired().HasMaxLength(255);
        builder.Property(a => a.UploadedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(a => a.Ticket)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Content).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(c => c.CommentType).HasMaxLength(255);
        builder.Property(c => c.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(c => c.Ticket)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TicketHistoryEntryConfiguration : IEntityTypeConfiguration<TicketHistoryEntry>
{
    public void Configure(EntityTypeBuilder<TicketHistoryEntry> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Action).IsRequired().HasMaxLength(255);
        builder.Property(h => h.Detail).HasColumnType("nvarchar(max)");
        builder.Property(h => h.Actor).HasMaxLength(255);
        builder.Property(h => h.Icon).HasMaxLength(255);
        builder.Property(h => h.TimestampUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(h => h.Ticket)
            .WithMany(t => t.History)
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TicketCapitalizationConfiguration : IEntityTypeConfiguration<TicketCapitalization>
{
    public void Configure(EntityTypeBuilder<TicketCapitalization> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.AssignmentType).IsRequired().HasMaxLength(255);
        builder.Property(c => c.IdStellantis).HasMaxLength(255);
        builder.Property(c => c.FullName).HasMaxLength(255);
        builder.Property(c => c.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(c => c.Ticket)
            .WithMany(t => t.Capitalizations)
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class UserTicketConfiguration : IEntityTypeConfiguration<UserTicket>
{
    public void Configure(EntityTypeBuilder<UserTicket> builder)
    {
        builder.ToTable("UserTickets");
        builder.HasKey(ut => new { ut.UserId, ut.TicketId });
        builder.Property(ut => ut.AssignmentType).HasMaxLength(255);

        builder.HasOne(ut => ut.User)
            .WithMany()
            .HasForeignKey(ut => ut.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ut => ut.Ticket)
            .WithMany()
            .HasForeignKey(ut => ut.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
