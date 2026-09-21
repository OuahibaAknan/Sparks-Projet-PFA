namespace Sparks.Api.Dtos;

public record StatusBreakdownPointDto(string Status, int Count);

public record DomainBreakdownPointDto(string Domain, int Count);

public record ResolutionTrendPointDto(string Label, double Hours);

public record ImpactTrendPointDto(string Label, double Value);

public record ImpactStatDto(string Label, string DeltaLabel, string Tone, List<ImpactTrendPointDto> Data);

public record AdminDashboardStatsDto(
    int TotalTickets,
    double TotalTicketsDeltaPct,
    int OpenTickets,
    double OpenTicketsDeltaPct,
    double AvgResolutionHours,
    double AvgResolutionDeltaPct,
    double RefusalRatePct,
    double RefusalRateDeltaPct,
    double AiAccuracyPct,
    double AiAccuracyDeltaPct,
    List<StatusBreakdownPointDto> StatusBreakdown,
    List<DomainBreakdownPointDto> DomainBreakdown,
    List<ResolutionTrendPointDto> ResolutionTrend
);
