using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sparks.Api.Models;

namespace Sparks.Api.Data.Configurations;

public class SourceSystemConfiguration : IEntityTypeConfiguration<SourceSystem>
{
    public void Configure(EntityTypeBuilder<SourceSystem> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(255);
        builder.Property(s => s.ApiEndpoint).HasMaxLength(255);
        builder.Property(s => s.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(s => s.Name).IsUnique();
    }
}

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Name).IsRequired().HasMaxLength(255);
        builder.Property(m => m.Application).HasMaxLength(255);
        builder.Property(m => m.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
    }
}

public class DomainConfiguration : IEntityTypeConfiguration<Domain>
{
    public void Configure(EntityTypeBuilder<Domain> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(255);
        builder.Property(d => d.Description).HasColumnType("nvarchar(max)");
        builder.Property(d => d.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(d => d.Name).IsUnique();
    }
}

public class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(255);
        builder.Property(s => s.Location).HasMaxLength(255);
        builder.Property(s => s.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
    }
}
