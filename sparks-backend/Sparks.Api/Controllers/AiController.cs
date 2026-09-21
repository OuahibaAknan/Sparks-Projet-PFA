using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparks.Api.Data;
using Sparks.Api.Dtos;
using Sparks.Api.Services;

namespace Sparks.Api.Controllers;

[ApiController]
[Authorize]
public class AiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly GeminiService _gemini;
    private readonly KnowledgeBaseService _knowledgeBase;
    private readonly TicketClassificationService _classification;

    public AiController(AppDbContext db, GeminiService gemini, KnowledgeBaseService knowledgeBase, TicketClassificationService classification)
    {
        _db = db;
        _gemini = gemini;
        _knowledgeBase = knowledgeBase;
        _classification = classification;
    }

    /// <summary>Automatic ticket qualification — analyzes a title/description and suggests the application, category, route (Generalist/Specialist) and assignment group.</summary>
    [AllowAnonymous]
    [HttpPost("api/tickets/classify")]
    public async Task<ActionResult<TicketClassificationDto>> Classify(ClassifyTicketRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) && string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { message = "Title or description is required." });

        var result = await _classification.ClassifyAsync(request.Title, request.Description);
        return result;
    }

    [HttpGet("api/tickets/{ticketId}/ai-metadata")]
    public async Task<ActionResult<AiTicketMetadataDto>> GetTicketMetadata(string ticketId)
    {
        var ticket = await _db.Tickets
            .Include(t => t.AiMetadata)
            .Include(t => t.AiSimilarTickets).ThenInclude(s => s.SimilarTicket).ThenInclude(st => st!.Domain)
            .Include(t => t.AiSuggestedSolutions).ThenInclude(s => s.SourceTicketRef)
            .Include(t => t.AiChatMessages)
            .FirstOrDefaultAsync(t => t.ReferenceKZ == ticketId);

        if (ticket?.AiMetadata is null) return NotFound(new { message = "No AI metadata for this ticket." });
        return ticket.ToAiMetadataDto();
    }

    [AllowAnonymous]
    [HttpPost("api/tickets/{ticketId}/ai-chat")]
    public async Task<ActionResult<object>> AskAssistant(string ticketId, AskAssistantDto request)
    {
        var ticket = await _db.Tickets.Include(t => t.Domain).FirstOrDefaultAsync(t => t.ReferenceKZ == ticketId);
        if (ticket is null) return NotFound(new { message = "Ticket not found." });

        var metadata = await _db.TicketAiMetadata.FirstOrDefaultAsync(m => m.TicketId == ticket.Id);
        if (metadata is null)
        {
            metadata = new Models.TicketAiMetadata { TicketId = ticket.Id, ClassifiedDomain = ticket.Domain?.Name };
            _db.TicketAiMetadata.Add(metadata);
        }

        _db.Add(new Models.AiChatMessage { TicketId = ticket.Id, Role = "user", Message = request.Question });

        var systemInstruction =
            "You are SmartTicket AI, a support assistant embedded in a PLM (Product Lifecycle Management) engineering support platform called SPARKS. " +
            "Help the support engineer working this ticket by answering their question clearly and concisely (2-4 sentences unless more detail is truly needed). " +
            $"Ticket {ticket.ReferenceKZ}: \"{ticket.Title}\". Domain: {ticket.Domain?.Name}. Priority: {ticket.Priority}. Status: {ticket.Status}. " +
            $"Description: {ticket.Description}";

        var kbMatches = _knowledgeBase.Search($"{ticket.Title} {ticket.Domain?.Name} {ticket.Description} {request.Question}");
        if (kbMatches.Count > 0)
        {
            var kbSection = string.Join("\n", kbMatches.Select(k =>
                $"- [{k.Category}] {k.Title}: {k.Content} Solution: {k.Solution}"));
            systemInstruction += "\n\nRelevant entries from the support knowledge base (use if helpful, ignore if not relevant):\n" + kbSection;
        }

        var reply = await _gemini.AskAsync(systemInstruction, request.Question);
        _db.Add(new Models.AiChatMessage { TicketId = ticket.Id, Role = "ai", Message = reply });

        await _db.SaveChangesAsync();
        return new { reply };
    }

    [HttpGet("api/statistics/ai-insights")]
    public async Task<ActionResult<AiInsightsStatsDto>> GetInsights()
    {
        var snapshot = await _db.AiInsightsSnapshots.FirstOrDefaultAsync();
        if (snapshot is null) return NotFound(new { message = "AI insights not seeded." });

        var recurring = await _db.AiTrendPoints.Where(p => p.SnapshotId == snapshot.Id && p.Kind == "recurring").OrderBy(p => p.Order)
            .Select(p => new TrendPointDto(p.Label, p.Value)).ToListAsync();
        var accuracy = await _db.AiTrendPoints.Where(p => p.SnapshotId == snapshot.Id && p.Kind == "accuracy").OrderBy(p => p.Order)
            .Select(p => new TrendPointDto(p.Label, p.Value)).ToListAsync();
        var queueItems = await _db.AiLowConfidenceItems.Include(q => q.Ticket).ThenInclude(t => t!.Domain).ToListAsync();
        var queue = queueItems.Select(q => q.ToDto()).ToList();

        var modelInfo = new ModelMonitoringInfoDto(
            snapshot.ModelVersion, snapshot.ModelLastUpdatedUtc, snapshot.AvgLatencyMs,
            snapshot.P95LatencyMs, snapshot.MonthlyTokens, snapshot.MonthlyCostUsd
        );

        return new AiInsightsStatsDto(
            snapshot.RoutingAccuracyPct, snapshot.RoutingAccuracyDeltaPct, snapshot.TicketsClassified,
            queue.Count, snapshot.EstimatedModelCostUsd, recurring, accuracy, queue, modelInfo
        );
    }

    /// <summary>Applies an admin-approved (or corrected) AI domain/route suggestion to the ticket and
    /// clears it from the low-confidence review queue. The AI's suggestion is never written to the
    /// ticket on its own — this explicit, admin-only action is the only path that updates it.</summary>
    [HttpPost("api/tickets/{ticketId}/ai-domain-review")]
    [Authorize(Roles = "Admin,TeamLead")]
    public async Task<ActionResult> ReviewDomainSuggestion(string ticketId, ApplyDomainReviewDto request)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Domain)
            .FirstOrDefaultAsync(t => t.ReferenceKZ == ticketId);
        if (ticket is null) return NotFound(new { message = "Ticket not found." });

        var domain = await _db.Domains.FirstOrDefaultAsync(d => d.Name == request.Domain);
        if (domain is null) return BadRequest(new { message = $"Unknown domain '{request.Domain}'." });

        var previousDomain = ticket.Domain?.Name ?? "none";
        ticket.Domain = domain;
        ticket.Level = request.Route;

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = $"{User.FindFirstValue("firstName")} {User.FindFirstValue("lastName")}";
        _db.TicketHistoryEntries.Add(new Models.TicketHistoryEntry
        {
            TicketId = ticket.Id,
            Action = $"AI domain suggestion approved: {previousDomain} -> {domain.Name} ({request.Route})",
            UserId = userId,
            Actor = userName,
            Icon = "check",
            IsAiGenerated = false,
        });

        var queueItem = await _db.AiLowConfidenceItems.FirstOrDefaultAsync(q => q.TicketId == ticket.Id);
        if (queueItem is not null) _db.AiLowConfidenceItems.Remove(queueItem);

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
