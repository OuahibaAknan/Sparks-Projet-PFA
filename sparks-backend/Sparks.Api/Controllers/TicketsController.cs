using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparks.Api.Data;
using Sparks.Api.Dtos;
using Sparks.Api.Models;
using Sparks.Api.Services;

namespace Sparks.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TicketsController(AppDbContext db)
    {
        _db = db;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentUserName => $"{User.FindFirstValue("firstName")} {User.FindFirstValue("lastName")}";
    private bool IsAdmin => User.IsInRole("Admin") || User.IsInRole("TeamLead");

    /// <summary>Blind pool — metadata only, never title/description. scope=specialist limits to escalated tickets.</summary>
    [HttpGet("pool")]
    public async Task<ActionResult<List<TicketBlindDto>>> GetPool([FromQuery] string scope = "generalist")
    {
        var query = scope == "specialist"
            ? _db.Tickets.Where(t => t.Status == TicketStatus.Escalated && t.AssigneeId == null)
            : _db.Tickets.Where(t => t.Status == TicketStatus.New && t.AssigneeId == null);

        var tickets = await query
            .Include(t => t.Domain)
            .Include(t => t.SourceSystem)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync();
        return tickets.Select(t => t.ToBlindDto()).ToList();
    }

    [HttpGet("mine")]
    public async Task<ActionResult<List<TicketBlindDto>>> GetMine([FromQuery] bool includeAll = false)
    {
        var userId = CurrentUserId;
        var query = _db.Tickets
            .Include(t => t.Domain)
            .Include(t => t.SourceSystem)
            .Where(t => t.AssigneeId == userId);

        if (!includeAll)
            query = query.Where(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);

        var tickets = await query.OrderByDescending(t => t.CreatedAtUtc).ToListAsync();
        return tickets.Select(t => t.ToBlindDto()).ToList();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketFullDto>> GetById(string id)
    {
        var ticket = await LoadFullAsync(id);
        if (ticket is null) return NotFound(new { message = "Ticket not found." });
        if (ticket.AssigneeId != CurrentUserId && !IsAdmin)
            return Forbid();
        return ticket.ToFullDto();
    }

    [HttpPost("{id}/accept")]
    public async Task<ActionResult<TicketFullDto>> Accept(string id)
    {
        var ticket = await LoadFullAsync(id);
        if (ticket is null) return NotFound(new { message = "Ticket not found." });
        if (ticket.AssigneeId != null) return Conflict(new { message = "Ticket already assigned." });

        ticket.Status = TicketStatus.Accepted;
        ticket.AssigneeId = CurrentUserId;
        AddHistory(ticket, "Accepted ticket from pool", "check", isAi: false);
        await _db.SaveChangesAsync();
        return ticket.ToFullDto();
    }

    [HttpPost("{id}/decline")]
    public async Task<IActionResult> Decline(string id)
    {
        var ticket = await LoadFullAsync(id);
        if (ticket is null) return NotFound(new { message = "Ticket not found." });
        AddHistory(ticket, "Declined from pool", "activity", isAi: false);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/escalate")]
    public async Task<ActionResult<TicketFullDto>> Escalate(string id)
    {
        var ticket = await LoadFullAsync(id);
        if (ticket is null) return NotFound(new { message = "Ticket not found." });
        if (ticket.AssigneeId != CurrentUserId && !IsAdmin) return Forbid();

        ticket.Status = TicketStatus.Escalated;
        ticket.AssigneeId = null;
        ticket.AiSuggestedRoute = SuggestedRoute.Specialist;
        ticket.Level = SuggestedRoute.Specialist;
        AddHistory(ticket, "Escalated to Specialist", "escalate", isAi: false);
        await NotifySpecialistsAsync(ticket, $"Ticket {ticket.ReferenceKZ} was escalated to the Specialist pool.");
        await _db.SaveChangesAsync();
        return ticket.ToFullDto();
    }

    [HttpPost("{id}/resolve")]
    public async Task<ActionResult<TicketFullDto>> Resolve(string id)
    {
        var ticket = await LoadFullAsync(id);
        if (ticket is null) return NotFound(new { message = "Ticket not found." });
        if (ticket.AssigneeId != CurrentUserId && !IsAdmin) return Forbid();

        ticket.Status = TicketStatus.Resolved;
        ticket.ResolvedAtUtc = DateTime.UtcNow;
        AddHistory(ticket, "Marked as Resolved", "check", isAi: false);
        await _db.SaveChangesAsync();
        return ticket.ToFullDto();
    }

    /// <summary>Personal KPIs for the dashboard — tickets resolved today and average resolution time, scoped to the current user.</summary>
    [HttpGet("my-stats")]
    public async Task<ActionResult<MyTicketStatsDto>> GetMyStats()
    {
        var userId = CurrentUserId;
        var todayUtc = DateTime.UtcNow.Date;

        var resolvedToday = await _db.Tickets.CountAsync(t =>
            t.AssigneeId == userId && t.ResolvedAtUtc != null && t.ResolvedAtUtc.Value.Date == todayUtc);

        var resolutionHours = await _db.Tickets
            .Where(t => t.AssigneeId == userId && t.ResolvedAtUtc != null)
            .Select(t => EF.Functions.DateDiffMinute(t.CreatedAtUtc, t.ResolvedAtUtc!.Value) / 60.0)
            .ToListAsync();

        var avgResolutionHours = resolutionHours.Count > 0 ? Math.Round(resolutionHours.Average(), 1) : 0;

        return new MyTicketStatsDto(resolvedToday, avgResolutionHours);
    }

    [HttpGet("status-counts")]
    public async Task<ActionResult<TicketStatusCountsDto>> GetStatusCounts([FromQuery] bool mineOnly = false)
    {
        var userId = CurrentUserId;
        var role = User.FindFirstValue(ClaimTypes.Role);

        var mineQuery = _db.Tickets.Where(t => t.AssigneeId == userId).Select(t => t.Status);

        if (mineOnly)
        {
            var mineOnlyStatuses = await mineQuery.ToListAsync();
            return BuildCounts(mineOnlyStatuses);
        }

        var poolQuery = role switch
        {
            "Specialist" => _db.Tickets.Where(t => t.AssigneeId == null && t.Status == TicketStatus.Escalated).Select(t => t.Status),
            "Polyvalent" => _db.Tickets.Where(t => t.AssigneeId == null && (t.Status == TicketStatus.New || t.Status == TicketStatus.Escalated)).Select(t => t.Status),
            _ => _db.Tickets.Where(t => t.AssigneeId == null && t.Status == TicketStatus.New).Select(t => t.Status),
        };

        var statuses = await mineQuery.Concat(poolQuery).ToListAsync();
        return BuildCounts(statuses);
    }

    private static TicketStatusCountsDto BuildCounts(List<TicketStatus> statuses)
    {
        int Count(params TicketStatus[] s) => statuses.Count(s.Contains);

        return new TicketStatusCountsDto(
            Open: Count(TicketStatus.New, TicketStatus.Accepted),
            InProgress: Count(TicketStatus.InProgress),
            Forwarded: Count(TicketStatus.Escalated),
            Done: Count(TicketStatus.Resolved),
            Closed: Count(TicketStatus.Closed),
            Cancelled: Count(TicketStatus.Cancelled)
        );
    }

    [HttpPost("{id}/close")]
    public async Task<ActionResult<TicketFullDto>> Close(string id)
    {
        var ticket = await LoadFullAsync(id);
        if (ticket is null) return NotFound(new { message = "Ticket not found." });
        if (ticket.AssigneeId != CurrentUserId && !IsAdmin) return Forbid();

        ticket.Status = TicketStatus.Closed;
        ticket.ClosingDate = DateTime.UtcNow;
        AddHistory(ticket, "Ticket closed", "check", isAi: false);
        await _db.SaveChangesAsync();
        return ticket.ToFullDto();
    }

    [HttpPost("{id}/comments")]
    public async Task<ActionResult<TicketFullDto>> AddComment(string id, AddCommentDto request)
    {
        var ticket = await LoadFullAsync(id);
        if (ticket is null) return NotFound(new { message = "Ticket not found." });
        if (ticket.AssigneeId != CurrentUserId && !IsAdmin) return Forbid();

        var comment = new TicketComment
        {
            TicketId = ticket.Id,
            UserId = CurrentUserId,
            Content = request.Message,
            User = await _db.Users.FindAsync(CurrentUserId),
        };
        // Ticket is already tracked with Comments eager-loaded (LoadFullAsync), so EF's
        // relationship fixup attaches this new comment to ticket.Comments automatically —
        // adding it to the navigation collection too would duplicate the entry.
        _db.TicketComments.Add(comment);

        if (ticket.AssigneeId.HasValue && ticket.AssigneeId.Value != CurrentUserId)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = ticket.AssigneeId.Value,
                TicketId = ticket.Id,
                Type = "comment",
                Message = $"{CurrentUserName} commented on {ticket.ReferenceKZ}.",
            });
        }

        await _db.SaveChangesAsync();
        return ticket.ToFullDto();
    }

    /// <summary>Admin-facing list — full visibility, filters + pagination.</summary>
    /// <summary>Read-only, blind (no title/description) paginated view of every ticket — available to any authenticated role.</summary>
    [HttpGet("all")]
    public async Task<ActionResult<PagedResultDto<TicketBlindDto>>> ListAllBlind([FromQuery] TicketFilterQuery filters)
    {
        var query = _db.Tickets
            .Include(t => t.Domain)
            .Include(t => t.SourceSystem)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filters.Status) && filters.Status != "All" && Enum.TryParse<TicketStatus>(filters.Status, out var status))
            query = query.Where(t => t.Status == status);
        if (!string.IsNullOrWhiteSpace(filters.Priority) && filters.Priority != "All" && Enum.TryParse<TicketPriority>(filters.Priority, out var priority))
            query = query.Where(t => t.Priority == priority);
        if (!string.IsNullOrWhiteSpace(filters.Domain) && filters.Domain != "All")
            query = query.Where(t => t.Domain != null && t.Domain.Name == filters.Domain);
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var q = filters.Search.ToLower();
            query = query.Where(t => t.ReferenceKZ.ToLower().Contains(q) || (t.Domain != null && t.Domain.Name.ToLower().Contains(q)));
        }

        var total = await query.CountAsync();
        var page = Math.Max(1, filters.Page);
        var pageSize = filters.PageSize <= 0 ? 8 : filters.PageSize;

        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<TicketBlindDto>(items.Select(t => t.ToBlindDto()).ToList(), total, page, pageSize);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,TeamLead")]
    public async Task<ActionResult<PagedResultDto<TicketFullDto>>> List([FromQuery] TicketFilterQuery filters)
    {
        var query = _db.Tickets
            .Include(t => t.Assignee)
            .Include(t => t.Domain)
            .Include(t => t.Site)
            .Include(t => t.SourceSystem)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filters.Status) && filters.Status != "All" && Enum.TryParse<TicketStatus>(filters.Status, out var status))
            query = query.Where(t => t.Status == status);
        if (!string.IsNullOrWhiteSpace(filters.Priority) && filters.Priority != "All" && Enum.TryParse<TicketPriority>(filters.Priority, out var priority))
            query = query.Where(t => t.Priority == priority);
        if (!string.IsNullOrWhiteSpace(filters.Domain) && filters.Domain != "All")
            query = query.Where(t => t.Domain != null && t.Domain.Name == filters.Domain);
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var q = filters.Search.ToLower();
            query = query.Where(t => t.ReferenceKZ.ToLower().Contains(q) || t.Title.ToLower().Contains(q)
                || (t.Domain != null && t.Domain.Name.ToLower().Contains(q)));
        }

        var total = await query.CountAsync();
        var page = Math.Max(1, filters.Page);
        var pageSize = filters.PageSize <= 0 ? 8 : filters.PageSize;

        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(t => t.Attachments)
            .Include(t => t.History).ThenInclude(h => h.User)
            .Include(t => t.Comments).ThenInclude(c => c.User)
            .ToListAsync();

        return new PagedResultDto<TicketFullDto>(items.Select(t => t.ToFullDto()).ToList(), total, page, pageSize);
    }

    private async Task<Ticket?> LoadFullAsync(string referenceKz) =>
        await _db.Tickets
            .Include(t => t.Assignee)
            .Include(t => t.Domain)
            .Include(t => t.Site)
            .Include(t => t.SourceSystem)
            .Include(t => t.Module)
            .Include(t => t.Attachments)
            .Include(t => t.History).ThenInclude(h => h.User)
            .Include(t => t.Comments).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(t => t.ReferenceKZ == referenceKz);

    private void AddHistory(Ticket ticket, string action, string icon, bool isAi)
    {
        var entry = new TicketHistoryEntry
        {
            TicketId = ticket.Id,
            Action = action,
            UserId = CurrentUserId,
            Actor = CurrentUserName,
            Icon = icon,
            IsAiGenerated = isAi,
        };
        // Add to the DbSet explicitly (not just the navigation collection) — Ticket is already
        // tracked as Unchanged here, and EF's reachability-based detection unreliably marks
        // collection-navigation-only additions as Modified instead of Added in that case.
        _db.TicketHistoryEntries.Add(entry);
        ticket.History.Add(entry);
    }

    private async Task NotifySpecialistsAsync(Ticket ticket, string message)
    {
        var specialists = await _db.Users
            .Where(u => u.Role == UserRole.Specialist && u.Status == UserStatus.Active)
            .ToListAsync();

        foreach (var specialist in specialists)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = specialist.Id,
                TicketId = ticket.Id,
                Type = "escalation",
                Message = message,
            });
        }
    }
}
