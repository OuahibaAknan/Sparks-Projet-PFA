namespace Sparks.Api.Dtos;

public record NotificationDto(Guid Id, string Type, string Message, bool Read, DateTime CreatedAt, string? TicketId);
