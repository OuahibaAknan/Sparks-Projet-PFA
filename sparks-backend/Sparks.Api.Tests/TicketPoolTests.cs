using Microsoft.AspNetCore.Mvc;
using Sparks.Api.Dtos;
using Sparks.Api.Models;
using Xunit;
using static Sparks.Api.Tests.TestSupport;

namespace Sparks.Api.Tests;

/// <summary>
/// Covers the blind-pool business rules on TicketsController: which tickets show up
/// in which pool, and the first-come-first-served guarantee on acceptance.
/// </summary>
public class TicketPoolTests
{
    [Fact]
    public async Task GetPool_Generalist_ReturnsOnlyUnassignedNewTickets()
    {
        await using var db = NewDb();
        var src = await SeedSourceSystemAsync(db);
        db.Tickets.AddRange(
            NewTicket("TKT-1", TicketStatus.New, src),                          // should appear
            NewTicket("TKT-2", TicketStatus.New, src, Guid.NewGuid()),          // already assigned — excluded
            NewTicket("TKT-3", TicketStatus.Escalated, src),                    // wrong scope — excluded
            NewTicket("TKT-4", TicketStatus.Resolved, src)                      // wrong status — excluded
        );
        await db.SaveChangesAsync();

        var controller = ControllerAs(db, Guid.NewGuid());
        var result = await controller.GetPool("generalist");

        var pool = Assert.IsType<List<TicketBlindDto>>(result.Value);
        Assert.Single(pool);
        Assert.Equal("TKT-1", pool[0].Id);
    }

    [Fact]
    public async Task GetPool_Specialist_ReturnsOnlyUnassignedEscalatedTickets()
    {
        await using var db = NewDb();
        var src = await SeedSourceSystemAsync(db);
        db.Tickets.AddRange(
            NewTicket("TKT-1", TicketStatus.New, src),
            NewTicket("TKT-2", TicketStatus.Escalated, src),                    // should appear
            NewTicket("TKT-3", TicketStatus.Escalated, src, Guid.NewGuid())     // already assigned — excluded
        );
        await db.SaveChangesAsync();

        var controller = ControllerAs(db, Guid.NewGuid());
        var result = await controller.GetPool("specialist");

        var pool = Assert.IsType<List<TicketBlindDto>>(result.Value);
        Assert.Single(pool);
        Assert.Equal("TKT-2", pool[0].Id);
    }

    [Fact]
    public async Task Accept_FirstCaller_Succeeds()
    {
        await using var db = NewDb();
        var src = await SeedSourceSystemAsync(db);
        db.Tickets.Add(NewTicket("TKT-1", TicketStatus.New, src));
        await db.SaveChangesAsync();

        var engineer = Guid.NewGuid();
        var controller = ControllerAs(db, engineer);

        var result = await controller.Accept("TKT-1");

        var dto = Assert.IsType<TicketFullDto>(result.Value);
        Assert.Equal(TicketStatus.Accepted, dto.Status);
    }

    [Fact]
    public async Task Accept_SecondCaller_GetsConflict()
    {
        await using var db = NewDb();
        var src = await SeedSourceSystemAsync(db);
        db.Tickets.Add(NewTicket("TKT-1", TicketStatus.New, src));
        await db.SaveChangesAsync();

        var firstEngineer = Guid.NewGuid();
        await ControllerAs(db, firstEngineer).Accept("TKT-1");

        var secondEngineer = Guid.NewGuid();
        var result = await ControllerAs(db, secondEngineer).Accept("TKT-1");

        Assert.IsType<ConflictObjectResult>(result.Result);
    }
}
