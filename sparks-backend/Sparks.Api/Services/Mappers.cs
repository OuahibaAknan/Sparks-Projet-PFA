using Sparks.Api.Dtos;
using Sparks.Api.Models;

namespace Sparks.Api.Services;

public static class Mappers
{
    public static int SlaMinutesRemaining(this Ticket t) =>
        (int)Math.Round((t.SlaDeadlineUtc - DateTime.UtcNow).TotalMinutes);

    private static string RequesterName(this Ticket t) =>
        $"{t.ApplicantFirstName} {t.ApplicantLastName}".Trim();

    public static TicketBlindDto ToBlindDto(this Ticket t) => new(
        t.ReferenceKZ, DateOnly.FromDateTime(t.CreatedAtUtc), t.Priority, t.Domain?.Name ?? "", t.Status,
        t.SlaMinutesRemaining(), t.AiSuggestedRoute, t.SourceSystem?.Name ?? ""
    );

    public static TicketFullDto ToFullDto(this Ticket t) => new(
        t.ReferenceKZ, DateOnly.FromDateTime(t.CreatedAtUtc), t.Priority, t.Domain?.Name ?? "", t.Status,
        t.SlaMinutesRemaining(), t.AiSuggestedRoute, t.SourceSystem?.Name ?? "",
        t.Title, t.Description, t.RequesterName(), t.Site?.Name ?? "",
        t.AssigneeId, t.Assignee is null ? null : $"{t.Assignee.FirstName} {t.Assignee.LastName}",
        t.PartNumber, t.EcrNumber,
        t.Attachments.Select(a => new TicketAttachmentDto(a.Id, a.FileName, a.Url, a.SizeKb)).ToList(),
        t.History.OrderBy(h => h.TimestampUtc).Select(h => new TicketHistoryEntryDto(
            h.Id, h.Action, h.Detail, h.User is null ? h.Actor : $"{h.User.FirstName} {h.User.LastName}", h.TimestampUtc, h.IsAiGenerated, h.Icon
        )).ToList(),
        t.Comments.OrderBy(c => c.CreatedAtUtc).Select(c => new TicketCommentDto(
            c.Id, c.User is null ? "" : $"{c.User.FirstName} {c.User.LastName}", c.User?.Initials ?? "", c.Content, c.CreatedAtUtc
        )).ToList(),
        t.ApplicantId, t.ApplicantFirstName, t.ApplicantLastName,
        t.ReferenceSTLA, t.Application, t.Module?.Name ?? "", t.SubModule,
        t.ClosingDate.HasValue ? DateOnly.FromDateTime(t.ClosingDate.Value) : null,
        t.ReactivityDuration.HasValue ? Math.Round(t.ReactivityDuration.Value.TotalHours, 1) : null,
        t.Level, t.Summary, t.JiraLink, t.EnglishAccepted
    );

    public static UserDto ToDto(this ApplicationUser u, int activeTickets) => new(
        u.Id, u.FirstName, u.LastName, u.Email ?? "", u.Role, u.Status, u.Availability,
        activeTickets, u.Initials, u.AvatarColor, u.CreatedAt
    );

    /// <summary>Builds the AI metadata DTO from a ticket loaded with its AiMetadata, AiSimilarTickets,
    /// AiSuggestedSolutions and AiChatMessages navigations.</summary>
    public static AiTicketMetadataDto ToAiMetadataDto(this Ticket t)
    {
        var m = t.AiMetadata;
        return new AiTicketMetadataDto(
            t.ReferenceKZ, m?.Summary ?? "", m?.ClassifiedDomain ?? "", (int)Math.Round(m?.Confidence ?? 0),
            t.AiSimilarTickets.Select(s => new SimilarTicketDto(
                s.SimilarTicket?.ReferenceKZ ?? "", s.SimilarTicket?.Domain?.Name ?? "", (int)Math.Round(s.Score)
            )).ToList(),
            t.AiSuggestedSolutions.OrderBy(s => s.Rank).Select(s => new SuggestedSolutionDto(
                s.Id, s.Rank, s.SolutionText, s.SourceTicketRef?.ReferenceKZ ?? "", s.NeedsVerification, s.Vote
            )).ToList(),
            t.AiChatMessages.OrderBy(c => c.TimestampUtc).Select(c => new AiChatMessageDto(
                c.Id, c.Role, c.Message, c.TimestampUtc
            )).ToList()
        );
    }

    public static LowConfidenceItemDto ToDto(this AiLowConfidenceItem q) => new(
        q.Ticket?.ReferenceKZ ?? "", q.Ticket?.Domain?.Name ?? "", q.SuggestedDomain, q.AiRoute, q.ConfidencePct
    );
}
