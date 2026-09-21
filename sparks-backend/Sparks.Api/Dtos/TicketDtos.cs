using System.ComponentModel.DataAnnotations;
using Sparks.Api.Models;

namespace Sparks.Api.Dtos;

public record TicketBlindDto(
    string Id,
    DateOnly CreatedAt,
    TicketPriority Priority,
    string Domain,
    TicketStatus Status,
    int SlaMinutesRemaining,
    SuggestedRoute AiSuggestedRoute,
    string SourceSystem
);

public record TicketAttachmentDto(Guid Id, string Name, string Url, int SizeKb);

public record TicketHistoryEntryDto(Guid Id, string Label, string? Detail, string Actor, DateTime Timestamp, bool IsAiGenerated, string Icon);

public record TicketCommentDto(Guid Id, string Author, string AuthorInitials, string Message, DateTime Timestamp);

public record TicketFullDto(
    string Id,
    DateOnly CreatedAt,
    TicketPriority Priority,
    string Domain,
    TicketStatus Status,
    int SlaMinutesRemaining,
    SuggestedRoute AiSuggestedRoute,
    string SourceSystem,
    string Title,
    string Description,
    string RequesterName,
    string Site,
    Guid? AssigneeId,
    string? AssigneeName,
    string? PartNumber,
    string? EcrNumber,
    List<TicketAttachmentDto> Attachments,
    List<TicketHistoryEntryDto> History,
    List<TicketCommentDto> Comments,
    string? ApplicantId,
    string? ApplicantFirstName,
    string? ApplicantLastName,
    string? ReferenceSTLA,
    string Application,
    string Module,
    string? SubModule,
    DateOnly? ClosingDate,
    double? ReactivityHours,
    SuggestedRoute Level,
    string? Summary,
    string? JiraLink,
    bool EnglishAccepted
);

public record PagedResultDto<T>(List<T> Items, int Total, int Page, int PageSize);

public record MyTicketStatsDto(int ResolvedToday, double AvgResolutionHours);

public record TicketStatusCountsDto(int Open, int InProgress, int Forwarded, int Done, int Closed, int Cancelled);

public record AddCommentDto([Required, MaxLength(5000)] string Message);

public class TicketFilterQuery
{
    /// <summary>Raw string so "All" (sent by the frontend's "All" filter chip) doesn't fail enum binding.</summary>
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Domain { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 8;
}
