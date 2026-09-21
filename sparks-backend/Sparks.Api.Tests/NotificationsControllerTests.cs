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

public class NotificationsControllerTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static NotificationsController CreateController(AppDbContext db, Guid userId)
    {
        var controller = new NotificationsController(db);
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
        return controller;
    }

    // ---- List ----

    [Fact]
    public async Task List_ReturnsOnlyCurrentUsersNotifications_NewestFirst()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        db.Notifications.Add(new Notification { UserId = userId, Type = "comment", Message = "First", CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10) });
        db.Notifications.Add(new Notification { UserId = userId, Type = "comment", Message = "Second", CreatedAtUtc = DateTime.UtcNow });
        db.Notifications.Add(new Notification { UserId = Guid.NewGuid(), Type = "comment", Message = "Someone else's" });
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.List();

        var items = Assert.IsType<List<NotificationDto>>(result.Value);
        Assert.Equal(2, items.Count);
        Assert.Equal("Second", items[0].Message);
    }

    // ---- UnreadCount ----

    [Fact]
    public async Task UnreadCount_CountsOnlyUnreadForCurrentUser()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        db.Notifications.Add(new Notification { UserId = userId, Type = "comment", Message = "A", Read = false });
        db.Notifications.Add(new Notification { UserId = userId, Type = "comment", Message = "B", Read = true });
        db.Notifications.Add(new Notification { UserId = Guid.NewGuid(), Type = "comment", Message = "C", Read = false });
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.UnreadCount();

        Assert.Equal(1, result.Value);
    }

    // ---- MarkRead ----

    [Fact]
    public async Task MarkRead_UnknownId_ReturnsNotFound()
    {
        var db = CreateInMemoryDb();
        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.MarkRead(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task MarkRead_BelongsToAnotherUser_ReturnsNotFound()
    {
        var db = CreateInMemoryDb();
        var notification = new Notification { UserId = Guid.NewGuid(), Type = "comment", Message = "A" };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.MarkRead(notification.Id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task MarkRead_OwnNotification_MarksReadAndReturnsNoContent()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var notification = new Notification { UserId = userId, Type = "comment", Message = "A", Read = false };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.MarkRead(notification.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.True(notification.Read);
    }

    // ---- MarkAllRead ----

    [Fact]
    public async Task MarkAllRead_MarksOnlyCurrentUsersUnreadNotifications()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var mine1 = new Notification { UserId = userId, Type = "comment", Message = "A", Read = false };
        var mine2 = new Notification { UserId = userId, Type = "comment", Message = "B", Read = false };
        var someoneElse = new Notification { UserId = Guid.NewGuid(), Type = "comment", Message = "C", Read = false };
        db.Notifications.AddRange(mine1, mine2, someoneElse);
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.MarkAllRead();

        Assert.IsType<NoContentResult>(result);
        Assert.True(mine1.Read);
        Assert.True(mine2.Read);
        Assert.False(someoneElse.Read);
    }
}
