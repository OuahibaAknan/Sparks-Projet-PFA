using Microsoft.EntityFrameworkCore;
using Sparks.Api.Controllers;
using Sparks.Api.Dtos;
using Sparks.Api.Models;
using Xunit;
using static Sparks.Api.Tests.TestSupport;

namespace Sparks.Api.Tests;

/// <summary>
/// Covers GetImpact — the live "Impact &amp; Benefits" metrics shown on the public landing page.
/// These pin the two least obvious design decisions: resolution rate is cumulative-to-date
/// (not a per-week creation cohort, which would show an artificial 0% for recent weeks), and
/// "favorable direction" is metric-specific — a rising trend is good for routing accuracy but
/// bad for the manual-qualification rate.
/// </summary>
public class StatisticsImpactTests
{
    private static Ticket TicketAt(DateTime createdAtUtc, SuggestedRoute level, SuggestedRoute aiRoute) => new()
    {
        ReferenceKZ = $"TKT-{Guid.NewGuid():N}",
        SourceSystemId = Guid.NewGuid(),
        Application = "CATIA",
        Title = "x",
        Description = "y",
        Priority = TicketPriority.Medium,
        CreatedAtUtc = createdAtUtc,
        Level = level,
        AiSuggestedRoute = aiRoute,
        SlaDeadlineUtc = createdAtUtc.AddHours(4),
    };

    [Fact]
    public async Task GetImpact_WithNoData_ReturnsAllZeroedStatsWithPositiveTone()
    {
        await using var db = NewDb();
        var controller = new StatisticsController(db);

        var result = await controller.GetImpact();

        var stats = Assert.IsType<List<ImpactStatDto>>(result.Value);
        Assert.Equal(4, stats.Count);
        Assert.All(stats, s =>
        {
            Assert.Equal("0%", s.DeltaLabel);
            Assert.Equal("positive", s.Tone); // first == last for every series, trivially "not worse"
        });

        // Resolution and routing have nothing to measure yet, so they're genuinely 0%.
        Assert.All(stats.Single(s => s.Label == "Faster Ticket Resolution").Data, p => Assert.Equal(0, p.Value));
        Assert.All(stats.Single(s => s.Label == "Smarter Ticket Routing").Data, p => Assert.Equal(0, p.Value));
        Assert.All(stats.Single(s => s.Label == "Reduced Manual Qualification").Data, p => Assert.Equal(0, p.Value));

        // Efficiency averages the three components, and "0% needing manual review" scores as a
        // full 100 in that average — so a genuinely empty dataset still reads as 33.3%, not 0%.
        Assert.All(stats.Single(s => s.Label == "Improved Support Efficiency").Data, p => Assert.Equal(33.3, p.Value));
    }

    [Fact]
    public async Task GetImpact_RoutingMatchPct_ReflectsLevelVsAiSuggestedRouteAgreement()
    {
        await using var db = NewDb();
        var yesterday = DateTime.UtcNow.AddDays(-1);
        db.Tickets.AddRange(
            TicketAt(yesterday, SuggestedRoute.Generalist, SuggestedRoute.Generalist),
            TicketAt(yesterday, SuggestedRoute.Generalist, SuggestedRoute.Generalist),
            TicketAt(yesterday, SuggestedRoute.Specialist, SuggestedRoute.Specialist),
            TicketAt(yesterday, SuggestedRoute.Generalist, SuggestedRoute.Specialist) // mismatch
        );
        await db.SaveChangesAsync();

        var stats = (await new StatisticsController(db).GetImpact()).Value!;
        var routing = stats.Single(s => s.Label == "Smarter Ticket Routing");

        Assert.Equal(75.0, routing.Data[^1].Value);
    }

    [Fact]
    public async Task GetImpact_ManualQualPct_CountsTicketsFlaggedLowConfidence()
    {
        await using var db = NewDb();
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var flagged = TicketAt(yesterday, SuggestedRoute.Generalist, SuggestedRoute.Generalist);
        db.Tickets.AddRange(
            flagged,
            TicketAt(yesterday, SuggestedRoute.Generalist, SuggestedRoute.Generalist),
            TicketAt(yesterday, SuggestedRoute.Generalist, SuggestedRoute.Generalist),
            TicketAt(yesterday, SuggestedRoute.Generalist, SuggestedRoute.Generalist)
        );
        db.AiLowConfidenceItems.Add(new AiLowConfidenceItem
        {
            TicketId = flagged.Id,
            SuggestedDomain = "CATIA",
            AiRoute = SuggestedRoute.Generalist,
            ConfidencePct = 40,
        });
        await db.SaveChangesAsync();

        var stats = (await new StatisticsController(db).GetImpact()).Value!;
        var manualQual = stats.Single(s => s.Label == "Reduced Manual Qualification");

        Assert.Equal(25.0, manualQual.Data[^1].Value);
    }

    [Fact]
    public async Task GetImpact_ResolutionOnTimePct_IsCumulativeAcrossWeeks_NotPerWeekCohort()
    {
        await using var db = NewDb();

        // Created 5 weeks ago (so its own creation week is long past), but only resolved yesterday.
        var ticket = TicketAt(DateTime.UtcNow.AddDays(-35), SuggestedRoute.Generalist, SuggestedRoute.Generalist);
        ticket.ResolvedAtUtc = DateTime.UtcNow.AddDays(-1);
        ticket.SlaDeadlineUtc = ticket.ResolvedAtUtc.Value.AddHours(4); // resolved before the deadline
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var stats = (await new StatisticsController(db).GetImpact()).Value!;
        var resolution = stats.Single(s => s.Label == "Faster Ticket Resolution");

        // Weeks before the resolution date have nothing resolved yet — cumulative rate is 0%,
        // not undefined or skipped. Only the week the resolution actually falls in (and any
        // week after it) shows the 100% on-time rate.
        Assert.Equal([0, 0, 0, 0, 0, 100], resolution.Data.Select(p => p.Value));
    }

    [Fact]
    public async Task GetImpact_DeltaToneDirection_IsMetricSpecific()
    {
        await using var db = NewDb();
        var earlyWeek = DateTime.UtcNow.AddDays(-38); // inside week 1 of the 6-week window
        var lastWeek = DateTime.UtcNow.AddDays(-1);   // inside week 6

        var earlyTickets = new[]
        {
            TicketAt(earlyWeek, SuggestedRoute.Generalist, SuggestedRoute.Generalist), // routing matches
            TicketAt(earlyWeek, SuggestedRoute.Generalist, SuggestedRoute.Generalist),
        };
        var lateTickets = new[]
        {
            TicketAt(lastWeek, SuggestedRoute.Generalist, SuggestedRoute.Specialist), // routing mismatches
            TicketAt(lastWeek, SuggestedRoute.Generalist, SuggestedRoute.Specialist),
        };
        db.Tickets.AddRange(earlyTickets.Concat(lateTickets));
        // Every late ticket needed manual review; no early ticket did — manual-qual rate rises.
        db.AiLowConfidenceItems.AddRange(lateTickets.Select(t => new AiLowConfidenceItem
        {
            TicketId = t.Id,
            SuggestedDomain = "CATIA",
            AiRoute = SuggestedRoute.Specialist,
            ConfidencePct = 40,
        }));
        await db.SaveChangesAsync();

        var stats = (await new StatisticsController(db).GetImpact()).Value!;

        // Routing accuracy fell (100% -> 0%) — a drop is unfavorable for a "higher is better" metric.
        var routing = stats.Single(s => s.Label == "Smarter Ticket Routing");
        Assert.Equal("negative", routing.Tone);
        Assert.Equal("-100%", routing.DeltaLabel);

        // Manual qualification rose (0% -> 100%) — a rise is unfavorable for a "lower is better" metric.
        var manualQual = stats.Single(s => s.Label == "Reduced Manual Qualification");
        Assert.Equal("negative", manualQual.Tone);
        Assert.Equal("+100%", manualQual.DeltaLabel);
    }
}
