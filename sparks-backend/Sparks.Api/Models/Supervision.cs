namespace Sparks.Api.Models;

public class KnowledgeBaseEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? DomainId { get; set; }
    public Domain? Domain { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    /// <summary>CSV or JSON list of search keywords.</summary>
    public string? Tags { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class SlaPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? DomainId { get; set; }
    public Domain? Domain { get; set; }
    public TicketPriority Priority { get; set; }
    public int DeadlineHours { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public bool Read { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? TicketId { get; set; }
    public Ticket? Ticket { get; set; }
}

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
