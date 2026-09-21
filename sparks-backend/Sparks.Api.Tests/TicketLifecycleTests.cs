using Microsoft.AspNetCore.Mvc;
using Sparks.Api.Dtos;
using Sparks.Api.Models;
using Xunit;
using static Sparks.Api.Tests.TestSupport;

namespace Sparks.Api.Tests;

/// <summary>
/// Covers the ticket state machine: accept → escalate → resolve → close, and the
/// authorization rule shared by every transition — only the current assignee, or an
/// Admin/TeamLead override, may act on a ticket.
/// </summary>
public class TicketLifecycleTests
{
    [Fact]
    public async Task Escalate_ByAssignee_ClearsAssigneeAndRoutesToSpecialist()
    {
        await using var db = NewDb();
        var src = await SeedSourceSystemAsync(db);
        var assignee = Guid.NewGuid();
        db.Tickets.Add(NewTicket("TKT-1", TicketStatus.Accepted, src, assignee));
        await db.SaveChangesAsync();

        var result = await ControllerAs(db, assignee).Escalate("TKT-1");

        var dto = Assert.IsType<TicketFullDto>(result.Value);
        Assert.Equal(TicketStatus.Escalated, dto.Status);
        Assert.Null(dto.AssigneeId);
    }

    [Fact]
    public async Task Escalate_ByUnrelatedEngineer_IsForbidden()
    {
        await using var db = NewDb();
        var src = await SeedSourceSystemAsync(db);
        var assignee = Guid.NewGuid();
        db.Tickets.Add(NewTicket("TKT-1", TicketStatus.Accepted, src, assignee));
        await db.SaveChangesAsync();

        var stranger = Guid.NewGuid();
        var result = await ControllerAs(db, stranger).Escalate("TKT-1");

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Escalate_ByAdmin_OverridesEvenWithoutBeingAssignee()
    {
        await using var db = NewDb();
        var src = await SeedSourceSystemAsync(db);
        var assignee = Guid.NewGuid();
        db.Tickets.Add(NewTicket("TKT-1", TicketStatus.Accepted, src, assignee));
        await db.SaveChangesAsync();

        var admin = Guid.NewGuid();
        var result = await ControllerAs(db, admin, "Admin").Escalate("TKT-1");

        var dto = Assert.IsType<TicketFullDto>(result.Value);
        Assert.Equal(TicketStatus.Escalated, dto.Status);
    }

    [Fact]
    public async Task Resolve_ByAssignee_StampsResolvedAtAndStatus()
    {
        await using var db = NewDb();
        var src = await SeedSourceSystemAsync(db);
        var assignee = Guid.NewGuid();
        var ticket = NewTicket("TKT-1", TicketStatus.Accepted, src, assignee);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var result = await ControllerAs(db, assignee).Resolve("TKT-1");

        var dto = Assert.IsType<TicketFullDto>(result.Value);
        Assert.Equal(TicketStatus.Resolved, dto.Status);

        var stored = await db.Tickets.FindAsync(ticket.Id);
        Assert.NotNull(stored!.ResolvedAtUtc);
    }

    [Fact]
    public async Task Resolve_ByUnrelatedEngineer_IsForbidden()
    {
        await using var db = NewDb();
        var src = await SeedSourceSystemAsync(db);
        var assignee = Guid.NewGuid();
        db.Tickets.Add(NewTicket("TKT-1", TicketStatus.Accepted, src, assignee));
        await db.SaveChangesAsync();

        var stranger = Guid.NewGuid();
        var result = await ControllerAs(db, stranger).Resolve("TKT-1");

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Close_ByAssignee_StampsClosingDateAndStatus()
    {
        await using var db = NewDb();
        var src = await SeedSourceSystemAsync(db);
        var assignee = Guid.NewGuid();
        db.Tickets.Add(NewTicket("TKT-1", TicketStatus.Resolved, src, assignee));
        await db.SaveChangesAsync();

        var result = await ControllerAs(db, assignee).Close("TKT-1");

        var dto = Assert.IsType<TicketFullDto>(result.Value);
        Assert.Equal(TicketStatus.Closed, dto.Status);
        Assert.NotNull(dto.ClosingDate);
    }

    [Fact]
    public async Task Close_ByUnrelatedEngineer_IsForbidden()
    {
        await using var db = NewDb();
        var src = await SeedSourceSystemAsync(db);
        var assignee = Guid.NewGuid();
        db.Tickets.Add(NewTicket("TKT-1", TicketStatus.Resolved, src, assignee));
        await db.SaveChangesAsync();

        var stranger = Guid.NewGuid();
        var result = await ControllerAs(db, stranger).Close("TKT-1");

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task FullLifecycle_NewToClosed_TransitionsThroughEveryStatus()
    {
        await using var db = NewDb();
        var src = await SeedSourceSystemAsync(db);
        db.Tickets.Add(NewTicket("TKT-1", TicketStatus.New, src));
        await db.SaveChangesAsync();

        var generalist = Guid.NewGuid();
        var afterAccept = (await ControllerAs(db, generalist).Accept("TKT-1")).Value!;
        Assert.Equal(TicketStatus.Accepted, afterAccept.Status);

        var afterEscalate = (await ControllerAs(db, generalist).Escalate("TKT-1")).Value!;
        Assert.Equal(TicketStatus.Escalated, afterEscalate.Status);

        var specialist = Guid.NewGuid();
        var afterReaccept = (await ControllerAs(db, specialist).Accept("TKT-1")).Value!;
        Assert.Equal(TicketStatus.Accepted, afterReaccept.Status);

        var afterResolve = (await ControllerAs(db, specialist).Resolve("TKT-1")).Value!;
        Assert.Equal(TicketStatus.Resolved, afterResolve.Status);

        var afterClose = (await ControllerAs(db, specialist).Close("TKT-1")).Value!;
        Assert.Equal(TicketStatus.Closed, afterClose.Status);
    }
}
