namespace Sparks.Api.Models;

public enum UserRole { Generalist, Specialist, Admin, TeamLead, Polyvalent }

public enum UserStatus { Active, Inactive }

public enum UserAvailability { Available, Away, Offline }

public enum TicketPriority { Low, Medium, High, Critical }

public enum TicketStatus { New, Accepted, InProgress, Escalated, Resolved, Closed, Cancelled }

public enum SuggestedRoute { Generalist, Specialist }

/// <summary>Line-level status for a <see cref="TicketCapitalization"/> entry.</summary>
public enum CapitalizationLineStatus { Active, Removed }
