namespace Sparks.Api.Models;

public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Human-readable internal SPARKS reference, e.g. "TKT-2840" — used as the public identifier everywhere (API routes, frontend).</summary>
    public string ReferenceKZ { get; set; } = "";
    /// <summary>External Stellantis-side reference, when the ticket originates from or is mirrored into a Stellantis system.</summary>
    public string? ReferenceSTLA { get; set; }

    public Guid SourceSystemId { get; set; }
    public SourceSystem? SourceSystem { get; set; }

    public string? ApplicantId { get; set; }
    public string? ApplicantFirstName { get; set; }
    public string? ApplicantLastName { get; set; }

    public string Application { get; set; } = "";
    /// <summary>FK into the mirrored external <see cref="Models.Application"/> catalog. Not enforced
    /// via a database constraint since it tracks an external system's identifiers, which may not
    /// always resolve to a local row.</summary>
    public Guid? ApplicationId { get; set; }
    public Guid? ModuleId { get; set; }
    public Module? Module { get; set; }
    public string? SubModule { get; set; }

    /// <summary>Legacy/external classification fields, mirrored as-is from the source system.</summary>
    public string? Category { get; set; }
    public string? Organization { get; set; }
    public string? Origin { get; set; }
    public string? StudyCell { get; set; }
    public DateTime? OpeningDate { get; set; }
    public DateTime? StartDate { get; set; }
    /// <summary>Creation date as recorded in the KZ/SPARKS system, distinct from <see cref="OpeningDate"/> in the source system.</summary>
    public DateTime? CreateDateKZ { get; set; }
    public string? EnglishTicketSummary { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.New;
    /// <summary>Support level the ticket requires — Generalist (first line) or Specialist (domain expert).</summary>
    public SuggestedRoute Level { get; set; }
    public DateTime SlaDeadlineUtc { get; set; }
    public SuggestedRoute AiSuggestedRoute { get; set; }

    public Guid? DomainId { get; set; }
    public Domain? Domain { get; set; }
    public Guid? SiteId { get; set; }
    public Site? Site { get; set; }

    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Summary { get; set; }
    public string? JiraLink { get; set; }
    public bool EnglishAccepted { get; set; }
    public TimeSpan? ReactivityDuration { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? ClosingDate { get; set; }
    public DateTime? LastSyncDate { get; set; }

    public Guid? AssigneeId { get; set; }
    public ApplicationUser? Assignee { get; set; }

    public string? PartNumber { get; set; }
    public string? EcrNumber { get; set; }

    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public ICollection<TicketHistoryEntry> History { get; set; } = new List<TicketHistoryEntry>();
    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public ICollection<TicketCapitalization> Capitalizations { get; set; } = new List<TicketCapitalization>();

    public TicketAiMetadata? AiMetadata { get; set; }
    public ICollection<AiSimilarTicket> AiSimilarTickets { get; set; } = new List<AiSimilarTicket>();
    public ICollection<AiSuggestedSolution> AiSuggestedSolutions { get; set; } = new List<AiSuggestedSolution>();
    public ICollection<AiChatMessage> AiChatMessages { get; set; } = new List<AiChatMessage>();
    public ICollection<AiLowConfidenceItem> AiLowConfidenceItems { get; set; } = new List<AiLowConfidenceItem>();
}

public class TicketAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public string FileName { get; set; } = "";
    public string Url { get; set; } = "#";
    public int SizeKb { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}

public class TicketHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public string Action { get; set; } = "";
    public string? Detail { get; set; }
    /// <summary>Null for system/AI-generated entries — see <see cref="Actor"/> for their display label.</summary>
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string Actor { get; set; } = "";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public bool IsAiGenerated { get; set; }
    public string Icon { get; set; } = "activity";
}

public class TicketComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string? CommentType { get; set; }
    public string Content { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Capitalization/handling record for how a ticket was staffed and processed (KT, N2, time spent, ...).</summary>
public class TicketCapitalization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public string AssignmentType { get; set; } = "";
    public string? IdStellantis { get; set; }
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string? FullName { get; set; }
    public bool CreateInSparks { get; set; }
    public DateTime? SparksProcessingDate { get; set; }
    public int? TimeSpentMinutes { get; set; }
    public bool CapitalizationStatus { get; set; }
    public CapitalizationLineStatus Status { get; set; } = CapitalizationLineStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Mirrors the external/legacy "UserTickets" table: which user worked an assignment stint on a
/// ticket, over what window, and whether it counts toward gamification. Composite key (UserId, TicketId).</summary>
public class UserTicket
{
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool CannotSolve { get; set; }
    public string? AssignmentType { get; set; }
    public bool IsMarkedDone { get; set; }
    public int? SpentTime { get; set; }
    public bool ToGamify { get; set; }
}
