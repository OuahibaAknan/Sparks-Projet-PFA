namespace Sparks.Api.Models;

/// <summary>1-1 AI classification sidecar for a ticket. Similar tickets, suggested solutions, chat and
/// low-confidence flags are separate top-level tables keyed directly off the ticket (see below), not nested here.</summary>
public class TicketAiMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public string? Summary { get; set; }
    public string? ClassifiedDomain { get; set; }
    public float? Confidence { get; set; }
}

public class AiSimilarTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public Guid SimilarTicketId { get; set; }
    public Ticket? SimilarTicket { get; set; }
    public float Score { get; set; }
}

public class AiSuggestedSolution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public string SolutionText { get; set; } = "";
    public int Rank { get; set; }
    /// <summary>The past resolved ticket this solution was mined from, if any.</summary>
    public Guid? SourceTicketRefId { get; set; }
    public Ticket? SourceTicketRef { get; set; }
    public bool NeedsVerification { get; set; } = true;
    public string? Vote { get; set; }
}

public class AiChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public string Role { get; set; } = "ai";
    public string Message { get; set; } = "";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Single-row snapshot backing the admin AI Insights screen. Kept as a purpose-built projection
/// (not a generic PeriodStart/PeriodEnd/MetricsJson blob) since it backs strongly-typed dashboard fields.</summary>
public class AiInsightsSnapshot
{
    public int Id { get; set; } = 1;
    public double RoutingAccuracyPct { get; set; }
    public double RoutingAccuracyDeltaPct { get; set; }
    public int TicketsClassified { get; set; }
    public double EstimatedModelCostUsd { get; set; }
    public string ModelVersion { get; set; } = "";
    public DateTime ModelLastUpdatedUtc { get; set; }
    public int AvgLatencyMs { get; set; }
    public int P95LatencyMs { get; set; }
    public string MonthlyTokens { get; set; } = "";
    public double MonthlyCostUsd { get; set; }

    public ICollection<AiTrendPoint> TrendPoints { get; set; } = new List<AiTrendPoint>();
}

public class AiTrendPoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int SnapshotId { get; set; } = 1;
    public AiInsightsSnapshot? Snapshot { get; set; }
    /// <summary>"recurring" or "accuracy" — which chart series this point belongs to.</summary>
    public string Kind { get; set; } = "";
    public string Label { get; set; } = "";
    public double Value { get; set; }
    public int Order { get; set; }
}

public class AiLowConfidenceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public string SuggestedDomain { get; set; } = "";
    public SuggestedRoute AiRoute { get; set; }
    public int ConfidencePct { get; set; }
    public string? Reason { get; set; }
}
