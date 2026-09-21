using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparks.Api.Data;
using Sparks.Api.Dtos;
using Sparks.Api.Models;

namespace Sparks.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,TeamLead")]
[Route("api/statistics")]
public class StatisticsController : ControllerBase
{
    private readonly AppDbContext _db;

    public StatisticsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("admin-dashboard")]
    public async Task<ActionResult<AdminDashboardStatsDto>> GetAdminDashboard()
    {
        var tickets = await _db.Tickets.Include(t => t.Domain).ToListAsync();
        var total = tickets.Count;
        var open = tickets.Count(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);

        var statusBreakdown = Enum.GetValues<TicketStatus>()
            .Select(s => new StatusBreakdownPointDto(s.ToString(), tickets.Count(t => t.Status == s)))
            .ToList();

        var domainBreakdown = tickets
            .GroupBy(t => t.Domain?.Name ?? "")
            .Select(g => new DomainBreakdownPointDto(g.Key, g.Count()))
            .OrderByDescending(d => d.Count)
            .ToList();

        // Deltas and the resolution trend require historical snapshots we don't track yet;
        // these mirror the same illustrative figures the frontend previously mocked.
        var resolutionTrend = new List<ResolutionTrendPointDto>
        {
            new("Mon", 4.8), new("Tue", 4.4), new("Wed", 3.9), new("Thu", 3.4),
            new("Fri", 3.0), new("Sat", 2.6), new("Sun", 2.4),
        };

        return new AdminDashboardStatsDto(
            total, 8.3, open, -5.1, 2.4, -31, 12.4, -2.1, 94.2, 1.8,
            statusBreakdown, domainBreakdown, resolutionTrend
        );
    }

    /// <summary>Public landing-page "Impact &amp; Benefits" metrics, computed live from ticket data over the
    /// last 6 weeks (bucketed by <see cref="Ticket.CreatedAtUtc"/>).</summary>
    [HttpGet("impact")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ImpactStatDto>>> GetImpact()
    {
        const int weekCount = 6;
        var periodStart = DateTime.UtcNow.Date.AddDays(-7 * weekCount);

        var tickets = await _db.Tickets
            .Where(t => t.CreatedAtUtc >= periodStart)
            .Select(t => new
            {
                t.Id,
                t.CreatedAtUtc,
                t.Level,
                t.AiSuggestedRoute,
            })
            .ToListAsync();

        // Resolution on-time rate is cumulative-to-date rather than a per-week cohort: a week's
        // just-created tickets are usually still open, so scoping to "created that week" would show
        // an artificial 0% until they happen to resolve. Cumulative avoids that cliff as long as
        // *something* has been resolved within the lookback window.
        var resolvedTickets = await _db.Tickets
            .Where(t => t.ResolvedAtUtc != null || t.ClosingDate != null)
            .Select(t => new { ResolvedAt = (t.ResolvedAtUtc ?? t.ClosingDate)!.Value, t.SlaDeadlineUtc })
            .ToListAsync();

        var lowConfidenceTicketIds = (await _db.AiLowConfidenceItems
            .Select(i => i.TicketId)
            .Distinct()
            .ToListAsync())
            .ToHashSet();

        var labels = new List<string>();
        var resolutionSeries = new List<double>();
        var routingSeries = new List<double>();
        var manualQualSeries = new List<double>();
        var efficiencySeries = new List<double>();

        for (var w = 0; w < weekCount; w++)
        {
            var weekStart = periodStart.AddDays(7 * w);
            var weekEnd = weekStart.AddDays(7);
            var weekTickets = tickets.Where(t => t.CreatedAtUtc >= weekStart && t.CreatedAtUtc < weekEnd).ToList();

            var resolvedToDate = resolvedTickets.Where(t => t.ResolvedAt < weekEnd).ToList();
            var resolutionOnTimePct = resolvedToDate.Count == 0
                ? 0
                : resolvedToDate.Count(t => t.ResolvedAt <= t.SlaDeadlineUtc) * 100.0 / resolvedToDate.Count;

            var routingMatchPct = weekTickets.Count == 0
                ? 0
                : weekTickets.Count(t => t.Level == t.AiSuggestedRoute) * 100.0 / weekTickets.Count;

            var manualQualPct = weekTickets.Count == 0
                ? 0
                : weekTickets.Count(t => lowConfidenceTicketIds.Contains(t.Id)) * 100.0 / weekTickets.Count;

            var efficiencyScore = (resolutionOnTimePct + routingMatchPct + (100 - manualQualPct)) / 3.0;

            labels.Add($"W{w + 1}");
            resolutionSeries.Add(Math.Round(resolutionOnTimePct, 1));
            routingSeries.Add(Math.Round(routingMatchPct, 1));
            manualQualSeries.Add(Math.Round(manualQualPct, 1));
            efficiencySeries.Add(Math.Round(efficiencyScore, 1));
        }

        return new List<ImpactStatDto>
        {
            BuildImpactStat("Faster Ticket Resolution", labels, resolutionSeries, higherIsBetter: true),
            BuildImpactStat("Smarter Ticket Routing", labels, routingSeries, higherIsBetter: true),
            BuildImpactStat("Reduced Manual Qualification", labels, manualQualSeries, higherIsBetter: false),
            BuildImpactStat("Improved Support Efficiency", labels, efficiencySeries, higherIsBetter: true),
        };
    }

    private static ImpactStatDto BuildImpactStat(string label, List<string> labels, List<double> series, bool higherIsBetter)
    {
        var first = series.Count > 0 ? series[0] : 0;
        var last = series.Count > 0 ? series[^1] : 0;
        var diff = Math.Round(last - first);
        var favorable = higherIsBetter ? last >= first : last <= first;
        var sign = diff > 0 ? "+" : diff < 0 ? "-" : "";
        var deltaLabel = $"{sign}{Math.Abs(diff):0}%";
        var tone = favorable ? "positive" : "negative";
        var data = labels.Zip(series, (l, v) => new ImpactTrendPointDto(l, v)).ToList();
        return new ImpactStatDto(label, deltaLabel, tone, data);
    }
}
