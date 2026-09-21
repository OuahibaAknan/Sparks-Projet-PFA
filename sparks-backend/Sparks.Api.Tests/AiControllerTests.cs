using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Sparks.Api.Controllers;
using Sparks.Api.Data;
using Sparks.Api.Dtos;
using Sparks.Api.Models;
using Sparks.Api.Services;
using Xunit;

namespace Sparks.Api.Tests;

/// <summary>GeminiService, KnowledgeBaseService and TicketClassificationService are concrete classes
/// with no interface and no virtual members, so none of their methods can be mocked as-is (see the
/// final summary for the minimal "add virtual" change that would fix this). These tests therefore
/// only cover the AiController branches that return before those services would be invoked (guard
/// clauses and DB-only lookups) — real, side-effect-light instances are wired up purely to satisfy
/// the constructor.</summary>
public class AiControllerTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AiController CreateController(AppDbContext db, string? role = null)
    {
        var config = new ConfigurationBuilder().Build();
        var gemini = new GeminiService(new HttpClient(), config, Mock.Of<ILogger<GeminiService>>(), new GeminiKeyRotator());

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(Path.GetTempPath());
        var knowledgeBase = new KnowledgeBaseService(mockEnv.Object, Mock.Of<ILogger<KnowledgeBaseService>>());

        var classification = new TicketClassificationService(gemini, knowledgeBase, Mock.Of<ILogger<TicketClassificationService>>());

        var controller = new AiController(db, gemini, knowledgeBase, classification);

        if (role is not null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new(ClaimTypes.Role, role),
                new("firstName", "Test"),
                new("lastName", "Admin"),
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
            };
        }

        return controller;
    }

    private static Ticket CreateTicket(string? referenceKz = null) => new()
    {
        ReferenceKZ = referenceKz ?? $"TKT-{Guid.NewGuid():N}"[..12],
        CreatedAtUtc = DateTime.UtcNow,
        SlaDeadlineUtc = DateTime.UtcNow.AddHours(4),
        Title = "Test ticket",
        Description = "Test description",
        Application = "CATIA",
        SourceSystem = new SourceSystem { Name = "TestSystem" },
    };

    // ---- Classify ----

    [Fact]
    public async Task Classify_EmptyTitleAndDescription_ReturnsBadRequest()
    {
        var db = CreateInMemoryDb();
        var controller = CreateController(db);

        var result = await controller.Classify(new ClassifyTicketRequestDto("", "  "));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ---- GetTicketMetadata ----

    [Fact]
    public async Task GetTicketMetadata_UnknownTicket_ReturnsNotFound()
    {
        var db = CreateInMemoryDb();
        var controller = CreateController(db);

        var result = await controller.GetTicketMetadata("TKT-9999");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTicketMetadata_TicketWithoutAiMetadata_ReturnsNotFound()
    {
        var db = CreateInMemoryDb();
        var ticket = CreateTicket();
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var result = await controller.GetTicketMetadata(ticket.ReferenceKZ);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTicketMetadata_TicketWithMetadata_ReturnsDto()
    {
        var db = CreateInMemoryDb();
        var ticket = CreateTicket();
        db.Tickets.Add(ticket);
        db.TicketAiMetadata.Add(new TicketAiMetadata { TicketId = ticket.Id, Summary = "Summary text", ClassifiedDomain = "BOM", Confidence = 87.4f });
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var result = await controller.GetTicketMetadata(ticket.ReferenceKZ);

        var dto = Assert.IsType<AiTicketMetadataDto>(result.Value);
        Assert.Equal("Summary text", dto.Summary);
        Assert.Equal("BOM", dto.ClassifiedDomain);
        Assert.Equal(87, dto.ClassificationConfidencePct);
    }

    // ---- AskAssistant ----

    [Fact]
    public async Task AskAssistant_UnknownTicket_ReturnsNotFound()
    {
        var db = CreateInMemoryDb();
        var controller = CreateController(db);

        var result = await controller.AskAssistant("TKT-9999", new AskAssistantDto("How do I fix this?"));

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ---- GetInsights ----

    [Fact]
    public async Task GetInsights_NoSnapshotSeeded_ReturnsNotFound()
    {
        var db = CreateInMemoryDb();
        var controller = CreateController(db);

        var result = await controller.GetInsights();

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetInsights_WithSnapshot_ReturnsAggregatedStats()
    {
        var db = CreateInMemoryDb();
        var snapshot = new AiInsightsSnapshot
        {
            RoutingAccuracyPct = 91.2,
            TicketsClassified = 420,
            ModelVersion = "gemini-flash-latest",
            MonthlyTokens = "1.2M",
        };
        db.AiInsightsSnapshots.Add(snapshot);
        db.AiTrendPoints.Add(new AiTrendPoint { SnapshotId = snapshot.Id, Kind = "recurring", Label = "Mon", Value = 3, Order = 0 });
        db.AiTrendPoints.Add(new AiTrendPoint { SnapshotId = snapshot.Id, Kind = "accuracy", Label = "Mon", Value = 91, Order = 0 });

        var lowConfidenceTicket = CreateTicket();
        db.Tickets.Add(lowConfidenceTicket);
        db.AiLowConfidenceItems.Add(new AiLowConfidenceItem { TicketId = lowConfidenceTicket.Id, SuggestedDomain = "BOM", ConfidencePct = 40 });
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var result = await controller.GetInsights();

        var dto = Assert.IsType<AiInsightsStatsDto>(result.Value);
        Assert.Equal(91.2, dto.RoutingAccuracyPct);
        Assert.Equal(420, dto.TicketsClassified);
        Assert.Single(dto.RecurringIssueTrend);
        Assert.Single(dto.RoutingAccuracyOverTime);
        Assert.Single(dto.LowConfidenceQueue);
        Assert.Equal(1, dto.LowConfidenceCount);
        Assert.Equal("gemini-flash-latest", dto.ModelInfo.Version);
    }

    [Fact]
    public async Task GetInsights_LowConfidenceItem_IncludesTicketsCurrentDomain()
    {
        var db = CreateInMemoryDb();
        db.AiInsightsSnapshots.Add(new AiInsightsSnapshot());

        var currentDomain = new Domain { Name = "CAD Data Sync" };
        var ticket = CreateTicket();
        ticket.Domain = currentDomain;
        db.Tickets.Add(ticket);
        db.AiLowConfidenceItems.Add(new AiLowConfidenceItem { TicketId = ticket.Id, SuggestedDomain = "BOM", ConfidencePct = 40 });
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var result = await controller.GetInsights();

        var dto = Assert.IsType<AiInsightsStatsDto>(result.Value);
        var item = Assert.Single(dto.LowConfidenceQueue);
        Assert.Equal("CAD Data Sync", item.CurrentDomain);
        Assert.Equal("BOM", item.SuggestedDomain);
    }

    // ---- ReviewDomainSuggestion ----

    [Fact]
    public async Task ReviewDomainSuggestion_UnknownTicket_ReturnsNotFound()
    {
        var db = CreateInMemoryDb();
        var controller = CreateController(db, "Admin");

        var result = await controller.ReviewDomainSuggestion("TKT-9999", new ApplyDomainReviewDto("BOM", SuggestedRoute.Generalist));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ReviewDomainSuggestion_UnknownDomain_ReturnsBadRequest()
    {
        var db = CreateInMemoryDb();
        var ticket = CreateTicket();
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        var controller = CreateController(db, "Admin");

        var result = await controller.ReviewDomainSuggestion(ticket.ReferenceKZ, new ApplyDomainReviewDto("NotARealDomain", SuggestedRoute.Generalist));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ReviewDomainSuggestion_ValidRequest_UpdatesTicketAndClearsQueue()
    {
        var db = CreateInMemoryDb();
        var oldDomain = new Domain { Name = "Document Control" };
        var newDomain = new Domain { Name = "BOM" };
        db.Domains.AddRange(oldDomain, newDomain);

        var ticket = CreateTicket();
        ticket.Domain = oldDomain;
        ticket.Level = SuggestedRoute.Generalist;
        db.Tickets.Add(ticket);
        db.AiLowConfidenceItems.Add(new AiLowConfidenceItem { TicketId = ticket.Id, SuggestedDomain = "BOM", ConfidencePct = 45 });
        await db.SaveChangesAsync();

        var controller = CreateController(db, "Admin");

        var result = await controller.ReviewDomainSuggestion(ticket.ReferenceKZ, new ApplyDomainReviewDto("BOM", SuggestedRoute.Specialist));

        Assert.IsType<NoContentResult>(result);
        var updated = await db.Tickets.Include(t => t.Domain).FirstAsync(t => t.Id == ticket.Id);
        Assert.Equal("BOM", updated.Domain!.Name);
        Assert.Equal(SuggestedRoute.Specialist, updated.Level);
        Assert.Empty(db.AiLowConfidenceItems);
        Assert.Single(db.TicketHistoryEntries);
    }
}
