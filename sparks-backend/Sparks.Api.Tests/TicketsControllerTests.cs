using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparks.Api.Controllers;
using Sparks.Api.Data;
using Sparks.Api.Dtos;
using Sparks.Api.Models;
using Xunit;

namespace Sparks.Api.Tests;

public class TicketsControllerTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static TicketsController CreateController(
        AppDbContext db, Guid userId, string role = "Generalist", string firstName = "Test", string lastName = "User")
    {
        var controller = new TicketsController(db);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role),
            new("firstName", firstName),
            new("lastName", lastName),
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
        return controller;
    }

    /// <summary>Ticket.SourceSystemId is a required (non-nullable) FK — EF Core's InMemory provider
    /// treats Include on a required navigation as an inner join, silently dropping rows that don't
    /// resolve, so every test ticket needs a real SourceSystem attached (not just a random Guid FK).</summary>
    private static Ticket CreateTicket(
        TicketStatus status = TicketStatus.New,
        Guid? assigneeId = null,
        DateTime? createdAtUtc = null,
        DateTime? resolvedAtUtc = null,
        string? referenceKz = null) => new()
    {
        ReferenceKZ = referenceKz ?? $"TKT-{Guid.NewGuid():N}"[..12],
        Status = status,
        AssigneeId = assigneeId,
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
        ResolvedAtUtc = resolvedAtUtc,
        SlaDeadlineUtc = DateTime.UtcNow.AddHours(4),
        Title = "Test ticket",
        Description = "Test description",
        Application = "CATIA",
        SourceSystem = new SourceSystem { Name = "TestSystem" },
    };

    // ---- GetPool ----

    [Fact]
    public async Task GetPool_GeneralistScope_ReturnsOnlyNewUnassignedTickets()
    {
        var db = CreateInMemoryDb();
        db.Tickets.Add(CreateTicket(TicketStatus.New));
        db.Tickets.Add(CreateTicket(TicketStatus.New, assigneeId: Guid.NewGuid()));
        db.Tickets.Add(CreateTicket(TicketStatus.Escalated));
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.GetPool("generalist");

        var tickets = Assert.IsType<List<TicketBlindDto>>(result.Value);
        Assert.Single(tickets);
    }

    [Fact]
    public async Task GetPool_SpecialistScope_ReturnsOnlyEscalatedUnassignedTickets()
    {
        var db = CreateInMemoryDb();
        db.Tickets.Add(CreateTicket(TicketStatus.New));
        db.Tickets.Add(CreateTicket(TicketStatus.Escalated));
        db.Tickets.Add(CreateTicket(TicketStatus.Escalated, assigneeId: Guid.NewGuid()));
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.GetPool("specialist");

        var tickets = Assert.IsType<List<TicketBlindDto>>(result.Value);
        Assert.Single(tickets);
    }

    // ---- GetMine ----

    [Fact]
    public async Task GetMine_Default_ExcludesResolvedAndClosed()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        db.Tickets.Add(CreateTicket(TicketStatus.InProgress, assigneeId: userId));
        db.Tickets.Add(CreateTicket(TicketStatus.Resolved, assigneeId: userId));
        db.Tickets.Add(CreateTicket(TicketStatus.Closed, assigneeId: userId));
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.GetMine(includeAll: false);

        var tickets = Assert.IsType<List<TicketBlindDto>>(result.Value);
        Assert.Single(tickets);
    }

    [Fact]
    public async Task GetMine_IncludeAll_ReturnsEverythingAssigned()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        db.Tickets.Add(CreateTicket(TicketStatus.InProgress, assigneeId: userId));
        db.Tickets.Add(CreateTicket(TicketStatus.Resolved, assigneeId: userId));
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.GetMine(includeAll: true);

        var tickets = Assert.IsType<List<TicketBlindDto>>(result.Value);
        Assert.Equal(2, tickets.Count);
    }

    // ---- GetById ----

    [Fact]
    public async Task GetById_UnknownReference_ReturnsNotFound()
    {
        var db = CreateInMemoryDb();
        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.GetById("TKT-9999");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_NotOwnerAndNotAdmin_ReturnsForbid()
    {
        var db = CreateInMemoryDb();
        var ticket = CreateTicket(assigneeId: Guid.NewGuid());
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid(), role: "Generalist");

        var result = await controller.GetById(ticket.ReferenceKZ);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetById_Owner_ReturnsTicket()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var ticket = CreateTicket(assigneeId: userId);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.GetById(ticket.ReferenceKZ);

        var dto = Assert.IsType<TicketFullDto>(result.Value);
        Assert.Equal(ticket.ReferenceKZ, dto.Id);
    }

    [Fact]
    public async Task GetById_Admin_CanViewTicketAssignedToSomeoneElse()
    {
        var db = CreateInMemoryDb();
        var ticket = CreateTicket(assigneeId: Guid.NewGuid());
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid(), role: "Admin");

        var result = await controller.GetById(ticket.ReferenceKZ);

        Assert.IsType<TicketFullDto>(result.Value);
    }

    // ---- Accept ----

    [Fact]
    public async Task Accept_UnknownReference_ReturnsNotFound()
    {
        var db = CreateInMemoryDb();
        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.Accept("TKT-9999");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Accept_AlreadyAssigned_ReturnsConflict()
    {
        var db = CreateInMemoryDb();
        var ticket = CreateTicket(assigneeId: Guid.NewGuid());
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.Accept(ticket.ReferenceKZ);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Accept_Unassigned_AssignsToCurrentUserAndAddsHistory()
    {
        var db = CreateInMemoryDb();
        var ticket = CreateTicket();
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var controller = CreateController(db, userId);

        var result = await controller.Accept(ticket.ReferenceKZ);

        var dto = Assert.IsType<TicketFullDto>(result.Value);
        Assert.Equal(userId, dto.AssigneeId);
        Assert.Equal(TicketStatus.Accepted, ticket.Status);
        Assert.Single(db.TicketHistoryEntries);
    }

    // ---- Decline ----

    [Fact]
    public async Task Decline_UnknownReference_ReturnsNotFound()
    {
        var db = CreateInMemoryDb();
        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.Decline("TKT-9999");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Decline_KnownReference_AddsHistoryAndReturnsNoContent()
    {
        var db = CreateInMemoryDb();
        var ticket = CreateTicket();
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.Decline(ticket.ReferenceKZ);

        Assert.IsType<NoContentResult>(result);
        Assert.Single(db.TicketHistoryEntries);
    }

    // ---- Escalate ----

    [Fact]
    public async Task Escalate_UnknownReference_ReturnsNotFound()
    {
        var db = CreateInMemoryDb();
        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.Escalate("TKT-9999");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Escalate_NotOwnerAndNotAdmin_ReturnsForbid()
    {
        var db = CreateInMemoryDb();
        var ticket = CreateTicket(assigneeId: Guid.NewGuid());
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.Escalate(ticket.ReferenceKZ);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Escalate_Owner_EscalatesTicketAndNotifiesActiveSpecialists()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var ticket = CreateTicket(assigneeId: userId);
        db.Tickets.Add(ticket);

        var activeSpecialist = new ApplicationUser
        {
            FirstName = "Sofia", LastName = "Reyes", Email = "sofia@alten.com",
            Role = UserRole.Specialist, Status = UserStatus.Active,
        };
        var inactiveSpecialist = new ApplicationUser
        {
            FirstName = "Ines", LastName = "Off", Email = "ines@alten.com",
            Role = UserRole.Specialist, Status = UserStatus.Inactive,
        };
        db.Users.AddRange(activeSpecialist, inactiveSpecialist);
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.Escalate(ticket.ReferenceKZ);

        var dto = Assert.IsType<TicketFullDto>(result.Value);
        Assert.Equal(TicketStatus.Escalated, dto.Status);
        Assert.Null(dto.AssigneeId);
        Assert.Equal(SuggestedRoute.Specialist, ticket.AiSuggestedRoute);
        Assert.Single(db.Notifications);
        Assert.Equal(activeSpecialist.Id, db.Notifications.Single().UserId);
    }

    // ---- Resolve ----

    [Fact]
    public async Task Resolve_NotOwnerAndNotAdmin_ReturnsForbid()
    {
        var db = CreateInMemoryDb();
        var ticket = CreateTicket(assigneeId: Guid.NewGuid());
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.Resolve(ticket.ReferenceKZ);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Resolve_Owner_MarksTicketResolved()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var ticket = CreateTicket(TicketStatus.InProgress, assigneeId: userId);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.Resolve(ticket.ReferenceKZ);

        var dto = Assert.IsType<TicketFullDto>(result.Value);
        Assert.Equal(TicketStatus.Resolved, dto.Status);
        Assert.NotNull(ticket.ResolvedAtUtc);
    }

    // ---- Close ----

    [Fact]
    public async Task Close_NotOwnerAndNotAdmin_ReturnsForbid()
    {
        var db = CreateInMemoryDb();
        var ticket = CreateTicket(assigneeId: Guid.NewGuid());
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.Close(ticket.ReferenceKZ);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Close_Owner_ClosesTicket()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var ticket = CreateTicket(TicketStatus.Resolved, assigneeId: userId);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.Close(ticket.ReferenceKZ);

        var dto = Assert.IsType<TicketFullDto>(result.Value);
        Assert.Equal(TicketStatus.Closed, dto.Status);
        Assert.NotNull(ticket.ClosingDate);
    }

    // ---- AddComment ----

    [Fact]
    public async Task AddComment_UnknownReference_ReturnsNotFound()
    {
        var db = CreateInMemoryDb();
        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.AddComment("TKT-9999", new AddCommentDto("Hello"));

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddComment_NotOwnerAndNotAdmin_ReturnsForbid()
    {
        var db = CreateInMemoryDb();
        var ticket = CreateTicket(assigneeId: Guid.NewGuid());
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.AddComment(ticket.ReferenceKZ, new AddCommentDto("Hello"));

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task AddComment_Owner_AddsCommentWithoutSelfNotification()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, FirstName = "Marc", LastName = "Dubois", Email = "marc@alten.com", Role = UserRole.Generalist };
        db.Users.Add(user);
        var ticket = CreateTicket(assigneeId: userId);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.AddComment(ticket.ReferenceKZ, new AddCommentDto("Working on it"));

        var dto = Assert.IsType<TicketFullDto>(result.Value);
        Assert.Single(dto.Comments, c => c.Message == "Working on it");
        Assert.Empty(db.Notifications);
    }

    [Fact]
    public async Task AddComment_AdminCommentingOnSomeoneElsesTicket_NotifiesAssignee()
    {
        var db = CreateInMemoryDb();
        var assigneeId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var admin = new ApplicationUser { Id = adminId, FirstName = "Amara", LastName = "Diallo", Email = "amara@alten.com", Role = UserRole.Admin };
        db.Users.Add(admin);
        var ticket = CreateTicket(assigneeId: assigneeId);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = CreateController(db, adminId, role: "Admin");

        var result = await controller.AddComment(ticket.ReferenceKZ, new AddCommentDto("Please check"));

        Assert.IsType<TicketFullDto>(result.Value);
        Assert.Single(db.Notifications);
        Assert.Equal(assigneeId, db.Notifications.Single().UserId);
    }

    // ---- GetMyStats ----

    /// <summary>GetMyStats' average-resolution-hours branch is computed via EF.Functions.DateDiffMinute,
    /// a SQL-Server-only translation with no InMemory client-eval fallback (throws InvalidOperationException
    /// as soon as any row has ResolvedAtUtc != null) — see final summary. Only the zero-resolved-tickets
    /// path, which never reaches that query, is testable here.</summary>
    [Fact]
    public async Task GetMyStats_NoResolvedTickets_ReturnsZeroCountAndZeroAverage()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        db.Tickets.Add(CreateTicket(TicketStatus.InProgress, assigneeId: userId));
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.GetMyStats();

        var dto = Assert.IsType<MyTicketStatsDto>(result.Value);
        Assert.Equal(0, dto.ResolvedToday);
        Assert.Equal(0, dto.AvgResolutionHours);
    }

    // ---- GetStatusCounts ----

    [Fact]
    public async Task GetStatusCounts_MineOnly_CountsOnlyAssignedToCurrentUser()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        db.Tickets.Add(CreateTicket(TicketStatus.New, assigneeId: userId));
        db.Tickets.Add(CreateTicket(TicketStatus.New));
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.GetStatusCounts(mineOnly: true);

        var dto = Assert.IsType<TicketStatusCountsDto>(result.Value);
        Assert.Equal(1, dto.Open);
    }

    [Fact]
    public async Task GetStatusCounts_SpecialistRole_IncludesEscalatedPool()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        db.Tickets.Add(CreateTicket(TicketStatus.Escalated));
        db.Tickets.Add(CreateTicket(TicketStatus.New));
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId, role: "Specialist");

        var result = await controller.GetStatusCounts(mineOnly: false);

        var dto = Assert.IsType<TicketStatusCountsDto>(result.Value);
        Assert.Equal(1, dto.Forwarded);
        Assert.Equal(0, dto.Open);
    }

    [Fact]
    public async Task GetStatusCounts_PolyvalentRole_IncludesNewAndEscalatedPool()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        db.Tickets.Add(CreateTicket(TicketStatus.Escalated));
        db.Tickets.Add(CreateTicket(TicketStatus.New));
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId, role: "Polyvalent");

        var result = await controller.GetStatusCounts(mineOnly: false);

        var dto = Assert.IsType<TicketStatusCountsDto>(result.Value);
        Assert.Equal(1, dto.Forwarded);
        Assert.Equal(1, dto.Open);
    }

    [Fact]
    public async Task GetStatusCounts_DefaultRole_OnlyIncludesNewPool()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        db.Tickets.Add(CreateTicket(TicketStatus.Escalated));
        db.Tickets.Add(CreateTicket(TicketStatus.New));
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId, role: "Generalist");

        var result = await controller.GetStatusCounts(mineOnly: false);

        var dto = Assert.IsType<TicketStatusCountsDto>(result.Value);
        Assert.Equal(0, dto.Forwarded);
        Assert.Equal(1, dto.Open);
    }

    // ---- ListAllBlind ----

    [Fact]
    public async Task ListAllBlind_FiltersByStatusAndPaginates()
    {
        var db = CreateInMemoryDb();
        for (var i = 0; i < 3; i++) db.Tickets.Add(CreateTicket(TicketStatus.New));
        db.Tickets.Add(CreateTicket(TicketStatus.Resolved));
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.ListAllBlind(new TicketFilterQuery { Status = "New", Page = 1, PageSize = 2 });

        var page = Assert.IsType<PagedResultDto<TicketBlindDto>>(result.Value);
        Assert.Equal(3, page.Total);
        Assert.Equal(2, page.Items.Count);
    }

    // ---- List ----

    [Fact]
    public async Task List_FiltersBySearchAndPaginates_ReturnsFullDtos()
    {
        var db = CreateInMemoryDb();
        var t1 = CreateTicket(TicketStatus.New, referenceKz: "TKT-0001");
        t1.Title = "BOM sync issue";
        var t2 = CreateTicket(TicketStatus.New, referenceKz: "TKT-0002");
        t2.Title = "Unrelated";
        db.Tickets.AddRange(t1, t2);
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid(), role: "Admin");

        var result = await controller.List(new TicketFilterQuery { Search = "BOM" });

        var page = Assert.IsType<PagedResultDto<TicketFullDto>>(result.Value);
        Assert.Equal(1, page.Total);
        Assert.Equal("TKT-0001", page.Items[0].Id);
    }
}
