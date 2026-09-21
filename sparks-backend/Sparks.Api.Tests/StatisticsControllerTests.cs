using Microsoft.EntityFrameworkCore;
using Sparks.Api.Controllers;
using Sparks.Api.Data;
using Sparks.Api.Dtos;
using Sparks.Api.Models;
using Xunit;

namespace Sparks.Api.Tests;

public class StatisticsControllerTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static StatisticsController CreateController(AppDbContext db) => new(db);

    private static Ticket CreateTicket(TicketStatus status, Domain? domain = null) => new()
    {
        ReferenceKZ = $"TKT-{Guid.NewGuid():N}"[..12],
        Status = status,
        Domain = domain,
        CreatedAtUtc = DateTime.UtcNow,
        SlaDeadlineUtc = DateTime.UtcNow.AddHours(4),
        Title = "Test ticket",
        Description = "Test description",
        Application = "CATIA",
        SourceSystem = new SourceSystem { Name = "TestSystem" },
    };

    [Fact]
    public async Task GetAdminDashboard_NoTickets_ReturnsZeroedTotals()
    {
        var db = CreateInMemoryDb();
        var controller = CreateController(db);

        var result = await controller.GetAdminDashboard();

        var dto = Assert.IsType<AdminDashboardStatsDto>(result.Value);
        Assert.Equal(0, dto.TotalTickets);
        Assert.Equal(0, dto.OpenTickets);
    }

    [Fact]
    public async Task GetAdminDashboard_ComputesTotalAndOpenCounts()
    {
        var db = CreateInMemoryDb();
        db.Tickets.Add(CreateTicket(TicketStatus.New));
        db.Tickets.Add(CreateTicket(TicketStatus.InProgress));
        db.Tickets.Add(CreateTicket(TicketStatus.Resolved));
        db.Tickets.Add(CreateTicket(TicketStatus.Closed));
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var result = await controller.GetAdminDashboard();

        var dto = Assert.IsType<AdminDashboardStatsDto>(result.Value);
        Assert.Equal(4, dto.TotalTickets);
        Assert.Equal(2, dto.OpenTickets);
    }

    [Fact]
    public async Task GetAdminDashboard_StatusBreakdown_CoversEveryEnumValue()
    {
        var db = CreateInMemoryDb();
        db.Tickets.Add(CreateTicket(TicketStatus.New));
        db.Tickets.Add(CreateTicket(TicketStatus.New));
        db.Tickets.Add(CreateTicket(TicketStatus.Escalated));
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var result = await controller.GetAdminDashboard();

        var dto = Assert.IsType<AdminDashboardStatsDto>(result.Value);
        Assert.Equal(Enum.GetValues<TicketStatus>().Length, dto.StatusBreakdown.Count);
        Assert.Equal(2, dto.StatusBreakdown.Single(s => s.Status == nameof(TicketStatus.New)).Count);
        Assert.Equal(1, dto.StatusBreakdown.Single(s => s.Status == nameof(TicketStatus.Escalated)).Count);
        Assert.Equal(0, dto.StatusBreakdown.Single(s => s.Status == nameof(TicketStatus.Closed)).Count);
    }

    [Fact]
    public async Task GetAdminDashboard_DomainBreakdown_GroupsByDomainAndTreatsMissingDomainAsEmptyKey()
    {
        var db = CreateInMemoryDb();
        var bom = new Domain { Name = "BOM" };
        db.Tickets.Add(CreateTicket(TicketStatus.New, bom));
        db.Tickets.Add(CreateTicket(TicketStatus.New, bom));
        db.Tickets.Add(CreateTicket(TicketStatus.New)); // no domain
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var result = await controller.GetAdminDashboard();

        var dto = Assert.IsType<AdminDashboardStatsDto>(result.Value);
        Assert.Equal(2, dto.DomainBreakdown.Count);
        Assert.Equal(2, dto.DomainBreakdown.Single(d => d.Domain == "BOM").Count);
        Assert.Equal(1, dto.DomainBreakdown.Single(d => d.Domain == "").Count);
    }
}
