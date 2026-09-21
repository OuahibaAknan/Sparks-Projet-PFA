using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sparks.Api.Models;

namespace Sparks.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Users & authentication
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Reference data
    public DbSet<SourceSystem> SourceSystems => Set<SourceSystem>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Domain> Domains => Set<Domain>();
    public DbSet<Site> Sites => Set<Site>();

    // Tickets
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketHistoryEntry> TicketHistoryEntries => Set<TicketHistoryEntry>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketCapitalization> TicketCapitalizations => Set<TicketCapitalization>();
    public DbSet<UserTicket> UserTickets => Set<UserTicket>();

    // External application catalog (mirrored)
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<UserApplication> UserApplications => Set<UserApplication>();

    // SmartTicket AI
    public DbSet<TicketAiMetadata> TicketAiMetadata => Set<TicketAiMetadata>();
    public DbSet<AiSimilarTicket> AiSimilarTickets => Set<AiSimilarTicket>();
    public DbSet<AiSuggestedSolution> AiSuggestedSolutions => Set<AiSuggestedSolution>();
    public DbSet<AiChatMessage> AiChatMessages => Set<AiChatMessage>();
    public DbSet<AiInsightsSnapshot> AiInsightsSnapshots => Set<AiInsightsSnapshot>();
    public DbSet<AiTrendPoint> AiTrendPoints => Set<AiTrendPoint>();
    public DbSet<AiLowConfidenceItem> AiLowConfidenceItems => Set<AiLowConfidenceItem>();

    // Referentiels & supervision
    public DbSet<KnowledgeBaseEntry> KnowledgeBaseEntries => Set<KnowledgeBaseEntry>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
