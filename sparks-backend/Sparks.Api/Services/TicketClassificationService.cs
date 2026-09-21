using System.Text.Json.Nodes;
using Sparks.Api.Dtos;
using Sparks.Api.Models;

namespace Sparks.Api.Services;

/// <summary>Automatic ticket qualification: identifies the concerned PLM application, category, Generalist/Specialist route, and a suggested assignment group.</summary>
public class TicketClassificationService
{
    private static readonly string[] Applications = ["CATIA", "ENOVIA", "DELMIA", "3DPassport", "SAP", "Teamcenter", "Autre"];
    private static readonly string[] Categories = ["ECR Workflow", "BOM", "CAD Data Sync", "PLM-ERP Integration", "Change Management", "Document Control", "Autre"];

    private readonly GeminiService _gemini;
    private readonly KnowledgeBaseService _knowledgeBase;
    private readonly ILogger<TicketClassificationService> _logger;

    public TicketClassificationService(GeminiService gemini, KnowledgeBaseService knowledgeBase, ILogger<TicketClassificationService> logger)
    {
        _gemini = gemini;
        _knowledgeBase = knowledgeBase;
        _logger = logger;
    }

    public async Task<TicketClassificationDto> ClassifyAsync(string title, string description)
    {
        var kbMatches = _knowledgeBase.Search($"{title} {description}");
        var kbContext = kbMatches.Count > 0
            ? "\n\nSimilar past tickets for reference:\n" + string.Join("\n", kbMatches.Select(k => $"- [{k.Category}] {k.Title}: {k.Content} => {k.Solution}"))
            : "";

        var systemInstruction =
            "You are SmartTicket AI, the automatic ticket qualification engine for SPARKS, a PLM (Product Lifecycle Management) support platform used by ALTEN. " +
            "Given a new support ticket's title and description, identify: " +
            "(1) the PLM application most likely concerned, " +
            "(2) the functional category, " +
            "(3) whether it should route to a Generalist (common, well-documented issue a first-line engineer can resolve) or a Specialist (complex, technical, domain-expert issue), " +
            "(4) a short suggested assignment group name (e.g. \"CATIA CAD Support\", \"SAP-PLM Integration Team\"), " +
            "(5) your confidence percentage (0-100), and (6) a one-sentence reasoning in French. " +
            "Respond only with the requested JSON, using only the allowed values for application/category/route." + kbContext;

        var prompt = $"Title: {title}\nDescription: {description}";

        var json = await _gemini.AskJsonAsync(systemInstruction, prompt, BuildSchema());
        if (json is null)
        {
            _logger.LogWarning("Ticket classification failed — no response from Gemini.");
            return Fallback();
        }

        try
        {
            var node = JsonNode.Parse(json);
            var application = node?["application"]?.GetValue<string>() ?? "Autre";
            var category = node?["category"]?.GetValue<string>() ?? "Autre";
            var routeStr = node?["route"]?.GetValue<string>() ?? nameof(SuggestedRoute.Generalist);
            var route = Enum.TryParse<SuggestedRoute>(routeStr, ignoreCase: true, out var parsedRoute) ? parsedRoute : SuggestedRoute.Generalist;
            var suggestedGroup = node?["suggestedGroup"]?.GetValue<string>() ?? "General Support";
            var confidence = node?["confidencePct"]?.GetValue<int>() ?? 50;
            var reasoning = node?["reasoning"]?.GetValue<string>() ?? "";

            return new TicketClassificationDto(application, category, route, suggestedGroup, Math.Clamp(confidence, 0, 100), reasoning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse ticket classification JSON: {Json}", json);
            return Fallback();
        }
    }

    private static TicketClassificationDto Fallback() =>
        new("Autre", "Autre", SuggestedRoute.Generalist, "General Support", 0, "Classification automatique indisponible — merci d'assigner manuellement.");

    private static JsonObject BuildSchema() => new()
    {
        ["type"] = "OBJECT",
        ["properties"] = new JsonObject
        {
            ["application"] = new JsonObject { ["type"] = "STRING", ["enum"] = new JsonArray(Applications.Select(a => (JsonNode)a).ToArray()) },
            ["category"] = new JsonObject { ["type"] = "STRING", ["enum"] = new JsonArray(Categories.Select(c => (JsonNode)c).ToArray()) },
            ["route"] = new JsonObject { ["type"] = "STRING", ["enum"] = new JsonArray("Generalist", "Specialist") },
            ["suggestedGroup"] = new JsonObject { ["type"] = "STRING" },
            ["confidencePct"] = new JsonObject { ["type"] = "INTEGER" },
            ["reasoning"] = new JsonObject { ["type"] = "STRING" },
        },
        ["required"] = new JsonArray("application", "category", "route", "suggestedGroup", "confidencePct", "reasoning"),
    };
}
