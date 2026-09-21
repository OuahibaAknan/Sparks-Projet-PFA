using Microsoft.EntityFrameworkCore;
using Sparks.Api.Data;
using Sparks.Api.Models;

namespace Sparks.Api.Services;

/// <summary>Periodically scans assigned, in-progress tickets for SLA risk and notifies the assignee —
/// once when the deadline is approaching, once more if it actually breaches. Runs on startup, then every
/// <c>SlaMonitor:IntervalMinutes</c>.</summary>
public class SlaMonitorService : BackgroundService
{
    private static readonly TicketStatus[] AtRiskStatuses = [TicketStatus.Accepted, TicketStatus.InProgress];

    private readonly IServiceProvider _services;
    private readonly ILogger<SlaMonitorService> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _warningThreshold;

    public SlaMonitorService(IServiceProvider services, ILogger<SlaMonitorService> logger, IConfiguration config)
    {
        _services = services;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(config.GetValue("SlaMonitor:IntervalMinutes", 5));
        _warningThreshold = TimeSpan.FromMinutes(config.GetValue("SlaMonitor:WarningThresholdMinutes", 120));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        do
        {
            try
            {
                await CheckSlaAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SLA monitor check failed.");
            }
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckSlaAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var warningCutoff = now.Add(_warningThreshold);

        var candidates = await db.Tickets
            .Where(t => t.AssigneeId != null && AtRiskStatuses.Contains(t.Status) && t.SlaDeadlineUtc <= warningCutoff)
            .ToListAsync(ct);

        var created = 0;
        foreach (var ticket in candidates)
        {
            var isBreached = ticket.SlaDeadlineUtc <= now;
            var type = isBreached ? "sla-breach" : "sla-warning";

            var alreadyNotified = await db.Notifications.AnyAsync(
                n => n.TicketId == ticket.Id && n.UserId == ticket.AssigneeId && n.Type == type, ct);
            if (alreadyNotified) continue;

            db.Notifications.Add(new Notification
            {
                UserId = ticket.AssigneeId!.Value,
                TicketId = ticket.Id,
                Type = type,
                Message = isBreached
                    ? $"{ticket.ReferenceKZ} has breached its SLA deadline."
                    : $"{ticket.ReferenceKZ} is approaching its SLA deadline.",
            });
            created++;
        }

        if (created > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("SLA monitor created {Count} notification(s).", created);
        }
    }
}
