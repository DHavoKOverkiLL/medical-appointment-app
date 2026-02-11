using MedicalAppointment.Api.Extensions;
using MedicalAppointment.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private const int DefaultTake = 50;
    private const int MaxTake = 200;
    private readonly AppDbContext _context;

    public NotificationController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications([FromQuery] bool unreadOnly = false, [FromQuery] int take = DefaultTake)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized("Invalid token subject.");
        }

        var safeTake = take <= 0 ? DefaultTake : Math.Min(take, MaxTake);
        var query = _context.UserNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(safeTake)
            .Select(n => new
            {
                n.UserNotificationId,
                n.UserId,
                n.AppointmentId,
                n.ActorUserId,
                n.Type,
                n.Title,
                n.Message,
                n.IsRead,
                n.CreatedAtUtc,
                n.ReadAtUtc
            })
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized("Invalid token subject.");
        }

        var unreadCount = await _context.UserNotifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        return Ok(new { unreadCount });
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized("Invalid token subject.");
        }

        var notification = await _context.UserNotifications
            .FirstOrDefaultAsync(n => n.UserNotificationId == notificationId && n.UserId == userId);

        if (notification == null)
        {
            return NotFound("Notification not found.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            notification.UserNotificationId,
            notification.IsRead,
            notification.ReadAtUtc
        });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized("Invalid token subject.");
        }

        var now = DateTime.UtcNow;
        var unreadNotifications = await _context.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = now;
        }

        if (unreadNotifications.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        return Ok(new { markedCount = unreadNotifications.Count });
    }
}
