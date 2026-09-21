using Sparks.Api.Models;
using Xunit;
using static Sparks.Api.Tests.GeminiTestSupport;

namespace Sparks.Api.Tests;

/// <summary>
/// TicketClassificationService talks to Google Gemini through GeminiService. These tests never
/// touch the network — a QueuedHttpMessageHandler stands in for the real Gemini endpoint, so we
/// can pin exactly how the service parses a well-formed reply and how it degrades when the AI
/// doesn't cooperate.
/// </summary>
public class TicketClassificationServiceTests
{
    private const string ValidClassificationJson =
        """{"application":"CATIA","category":"CAD Data Sync","route":"Specialist","suggestedGroup":"CATIA CAD Support","confidencePct":88,"reasoning":"Erreur de synchronisation CAD recurrente."}""";

    [Fact]
    public async Task ClassifyAsync_WithWellFormedReply_ReturnsParsedClassification()
    {
        var (gemini, _) = BuildGemini(responses: GeminiSuccess(ValidClassificationJson));
        var service = BuildClassificationService(gemini);

        var result = await service.ClassifyAsync("Sync CAD en echec", "Le modele ne se synchronise plus depuis ce matin.");

        Assert.Equal("CATIA", result.Application);
        Assert.Equal("CAD Data Sync", result.Category);
        Assert.Equal(SuggestedRoute.Specialist, result.Route);
        Assert.Equal(88, result.ConfidencePct);
    }

    [Fact]
    public async Task ClassifyAsync_WhenGeminiHasNoCandidates_FallsBackToManualRouting()
    {
        var (gemini, _) = BuildGemini(responses: GeminiSuccess(""));
        var service = BuildClassificationService(gemini);

        var result = await service.ClassifyAsync("Titre", "Description");

        Assert.Equal("Autre", result.Application);
        Assert.Equal(SuggestedRoute.Generalist, result.Route);
        Assert.Equal(0, result.ConfidencePct);
    }

    [Fact]
    public async Task ClassifyAsync_WhenGeminiReturnsMalformedJson_FallsBackToManualRouting()
    {
        var (gemini, _) = BuildGemini(responses: GeminiSuccess("this is not json"));
        var service = BuildClassificationService(gemini);

        var result = await service.ClassifyAsync("Titre", "Description");

        Assert.Equal(SuggestedRoute.Generalist, result.Route);
        Assert.Equal(0, result.ConfidencePct);
    }

    [Fact]
    public async Task ClassifyAsync_WhenGeminiIsUnreachable_FallsBackToManualRouting()
    {
        var (gemini, _) = BuildGemini(responses: GeminiError(System.Net.HttpStatusCode.InternalServerError));
        var service = BuildClassificationService(gemini);

        var result = await service.ClassifyAsync("Titre", "Description");

        Assert.Equal(SuggestedRoute.Generalist, result.Route);
        Assert.Equal(0, result.ConfidencePct);
        Assert.Contains("manuellement", result.Reasoning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ClassifyAsync_OnQuotaError_RotatesToNextKeyAndSucceeds()
    {
        var configOverrides = new Dictionary<string, string?>
        {
            ["Gemini:ApiKeys:0"] = "key-A",
            ["Gemini:ApiKeys:1"] = "key-B",
        };
        var (gemini, handler) = BuildGemini(
            configOverrides,
            GeminiError(System.Net.HttpStatusCode.TooManyRequests), // key-A is rate-limited
            GeminiSuccess(ValidClassificationJson));                // key-B succeeds

        var service = BuildClassificationService(gemini);
        var result = await service.ClassifyAsync("Titre", "Description");

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("key=key-A", handler.Requests[0].RequestUri!.Query);
        Assert.Contains("key=key-B", handler.Requests[1].RequestUri!.Query);
        Assert.Equal(SuggestedRoute.Specialist, result.Route);
    }
}
