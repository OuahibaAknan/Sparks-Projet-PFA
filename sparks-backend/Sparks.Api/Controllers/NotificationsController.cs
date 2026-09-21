using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparks.Api.Data;
using Sparks.Api.Dtos;

namespace Sparks.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotificationsController(AppDbContext db)
    {
        _db = db;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> List()
    {
        var items = await _db.Notifications
            .Include(n => n.Ticket)
            .Where(n => n.UserId == CurrentUserId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(50)
            .ToListAsync();

        return items.Select(n => new NotificationDto(n.Id, n.Type, n.Message, n.Read, n.CreatedAtUtc, n.Ticket?.ReferenceKZ)).ToList();
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> UnreadCount() =>
        await _db.Notifications.CountAsync(n => n.UserId == CurrentUserId && !n.Read);

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == CurrentUserId);
        if (notification is null) return NotFound();

        notification.Read = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var unread = await _db.Notifications.Where(n => n.UserId == CurrentUserId && !n.Read).ToListAsync();
        foreach (var n in unread) n.Read = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
