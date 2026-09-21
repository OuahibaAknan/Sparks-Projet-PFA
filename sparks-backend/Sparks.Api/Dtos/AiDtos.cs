using Sparks.Api.Models;

namespace Sparks.Api.Dtos;

public record SimilarTicketDto(string TicketId, string Domain, int SimilarityPct);

public record SuggestedSolutionDto(Guid Id, int Rank, string Text, string SourceTicketId, bool NeedsVerification, string? Vote);

public record AiChatMessageDto(Guid Id, string Sender, string Text, DateTime Timestamp);

public record AiTicketMetadataDto(
    string TicketId,
    string Summary,
    string ClassifiedDomain,
    int ClassificationConfidencePct,
    List<SimilarTicketDto> SimilarTickets,
    List<SuggestedSolutionDto> SuggestedSolutions,
    List<AiChatMessageDto> Chat
);

public record AskAssistantDto(string Question);

public record ClassifyTicketRequestDto(string Title, string Description);

public record TicketClassificationDto(
    string Application,
    string Category,
    SuggestedRoute Route,
    string SuggestedGroup,
    int ConfidencePct,
    string Reasoning
);

public record TrendPointDto(string Label, double Value);

public record LowConfidenceItemDto(string TicketId, string CurrentDomain, string SuggestedDomain, SuggestedRoute AiRoute, int ConfidencePct);

/// <summary>Admin-submitted decision on an AI domain/route suggestion — either the AI's own values
/// (Approve) or an admin-edited pair (Correct). Never applied automatically.</summary>
public record ApplyDomainReviewDto(string Domain, SuggestedRoute Route);

public record ModelMonitoringInfoDto(string Version, DateTime LastUpdated, int AvgLatencyMs, int P95LatencyMs, string MonthlyTokens, double MonthlyCostUsd);

public record AiInsightsStatsDto(
    double RoutingAccuracyPct,
    double RoutingAccuracyDeltaPct,
    int TicketsClassified,
    int LowConfidenceCount,
    double EstimatedModelCostUsd,
    List<TrendPointDto> RecurringIssueTrend,
    List<TrendPointDto> RoutingAccuracyOverTime,
    List<LowConfidenceItemDto> LowConfidenceQueue,
    ModelMonitoringInfoDto ModelInfo
);
