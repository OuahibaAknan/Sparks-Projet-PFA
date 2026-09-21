using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sparks.Api.Controllers;
using Sparks.Api.Data;
using Sparks.Api.Models;

namespace Sparks.Api.Tests;

/// <summary>Shared fixtures for controller tests: an isolated in-memory DbContext, a fake
/// authenticated user, and minimal valid entities.</summary>
internal static class TestSupport
{
    public static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    /// <summary>
    /// Ticket.SourceSystem is a required relationship — the EF Core InMemory provider treats
    /// Include() on a required navigation as an inner join, silently dropping rows whose FK
    /// doesn't resolve to a real principal. Tests must seed a real SourceSystem row.
    /// </summary>
    public static async Task<Guid> SeedSourceSystemAsync(AppDbContext db)
    {
        var sourceSystem = new SourceSystem { Name = "AIDE" };
        db.SourceSystems.Add(sourceSystem);
        await db.SaveChangesAsync();
        return sourceSystem.Id;
    }

    public static Ticket NewTicket(string reference, TicketStatus status, Guid sourceSystemId, Guid? assigneeId = null) => new()
    {
        ReferenceKZ = reference,
        SourceSystemId = sourceSystemId,
        Application = "CATIA",
        Title = "Some issue",
        Description = "Some description",
        Priority = TicketPriority.Medium,
        Status = status,
        AssigneeId = assigneeId,
        SlaDeadlineUtc = DateTime.UtcNow.AddHours(4),
    };

    public static ClaimsPrincipal PrincipalFor(Guid userId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("firstName", "Test"),
            new("lastName", "User"),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    public static TicketsController ControllerAs(AppDbContext db, Guid userId, params string[] roles) => new(db)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = PrincipalFor(userId, roles) } },
    };

    /// <summary>
    /// Controllers instantiated directly via `new` (rather than through the MVC pipeline) have no
    /// HttpContext, so any read of `User` throws a NullReferenceException. This stubs a minimal
    /// authenticated HttpContext so controller actions that log the acting user's identity can run.
    /// </summary>
    public static void SetFakeHttpContext(ControllerBase controller, string userEmail = "admin@alten.com")
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, userEmail) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    /// <summary>A real EF-Core-backed UserManager against the in-memory AppDbContext — enough to
    /// exercise FindByIdAsync/UpdateAsync without a real database, with validators disabled since
    /// these tests aren't exercising Identity's own validation rules.</summary>
    public static UserManager<ApplicationUser> BuildUserManager(AppDbContext db)
    {
        var store = new UserStore<ApplicationUser, IdentityRole<Guid>, AppDbContext, Guid>(db);
        return new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!, // no DI container needed for FindByIdAsync/UpdateAsync in these tests
            NullLogger<UserManager<ApplicationUser>>.Instance
        );
    }
}
