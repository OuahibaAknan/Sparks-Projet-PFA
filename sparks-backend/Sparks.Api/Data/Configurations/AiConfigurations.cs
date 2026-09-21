using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sparks.Api.Models;

namespace Sparks.Api.Data.Configurations;

public class TicketAiMetadataConfiguration : IEntityTypeConfiguration<TicketAiMetadata>
{
    public void Configure(EntityTypeBuilder<TicketAiMetadata> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Summary).HasColumnType("nvarchar(max)");
        builder.Property(m => m.ClassifiedDomain).HasMaxLength(255);

        builder.HasOne(m => m.Ticket)
            .WithOne(t => t.AiMetadata)
            .HasForeignKey<TicketAiMetadata>(m => m.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.TicketId).IsUnique();
    }
}

public class AiSimilarTicketConfiguration : IEntityTypeConfiguration<AiSimilarTicket>
{
    public void Configure(EntityTypeBuilder<AiSimilarTicket> builder)
    {
        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.Ticket)
            .WithMany(t => t.AiSimilarTickets)
            .HasForeignKey(s => s.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.SimilarTicket)
            .WithMany()
            .HasForeignKey(s => s.SimilarTicketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AiSuggestedSolutionConfiguration : IEntityTypeConfiguration<AiSuggestedSolution>
{
    public void Configure(EntityTypeBuilder<AiSuggestedSolution> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SolutionText).HasColumnType("nvarchar(max)");
        builder.Property(s => s.Vote).HasMaxLength(50);

        builder.HasOne(s => s.Ticket)
            .WithMany(t => t.AiSuggestedSolutions)
            .HasForeignKey(s => s.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.SourceTicketRef)
            .WithMany()
            .HasForeignKey(s => s.SourceTicketRefId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AiChatMessageConfiguration : IEntityTypeConfiguration<AiChatMessage>
{
    public void Configure(EntityTypeBuilder<AiChatMessage> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Role).HasMaxLength(50);
        builder.Property(c => c.Message).HasColumnType("nvarchar(max)");
        builder.Property(c => c.TimestampUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(c => c.Ticket)
            .WithMany(t => t.AiChatMessages)
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AiInsightsSnapshotConfiguration : IEntityTypeConfiguration<AiInsightsSnapshot>
{
    public void Configure(EntityTypeBuilder<AiInsightsSnapshot> builder)
    {
        builder.HasKey(a => a.Id);
        // Single-row snapshot — Id is always 1, set explicitly by the seeder rather than identity-generated.
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.ModelVersion).HasMaxLength(255);
        builder.Property(a => a.MonthlyTokens).HasMaxLength(255);
    }
}

public class AiTrendPointConfiguration : IEntityTypeConfiguration<AiTrendPoint>
{
    public void Configure(EntityTypeBuilder<AiTrendPoint> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Kind).HasMaxLength(50);
        builder.Property(p => p.Label).HasMaxLength(255);

        builder.HasOne(p => p.Snapshot)
            .WithMany(s => s.TrendPoints)
            .HasForeignKey(p => p.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AiLowConfidenceItemConfiguration : IEntityTypeConfiguration<AiLowConfidenceItem>
{
    public void Configure(EntityTypeBuilder<AiLowConfidenceItem> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.SuggestedDomain).HasMaxLength(255);
        builder.Property(q => q.Reason).HasColumnType("nvarchar(max)");

        builder.HasOne(q => q.Ticket)
            .WithMany(t => t.AiLowConfidenceItems)
            .HasForeignKey(q => q.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
