using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Sparks.Api.Services;

namespace Sparks.Api.Tests;

/// <summary>Points KnowledgeBaseService at a directory with no knowledge_base.json, so Search()
/// always returns empty — the service is built to handle a missing file gracefully.</summary>
internal class FakeWebHostEnvironment : IWebHostEnvironment
{
    public string ContentRootPath { get; set; } = Path.GetTempPath();
    public string ApplicationName { get; set; } = "Sparks.Api.Tests";
    public string EnvironmentName { get; set; } = "Testing";
    public string WebRootPath { get; set; } = Path.GetTempPath();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}

/// <summary>Replays a fixed queue of HTTP responses instead of making real network calls —
/// used to drive GeminiService without ever reaching the real Gemini API.</summary>
internal class QueuedHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses;
    public List<HttpRequestMessage> Requests { get; } = [];

    public QueuedHttpMessageHandler(params HttpResponseMessage[] responses)
    {
        _responses = new Queue<HttpResponseMessage>(responses);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var response = _responses.Count > 0 ? _responses.Dequeue() : new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);
        return Task.FromResult(response);
    }
}

internal static class GeminiTestSupport
{
    /// <summary>Builds the raw Gemini API response envelope for a successful call whose model text is <paramref name="text"/>.</summary>
    public static HttpResponseMessage GeminiSuccess(string text)
    {
        var body = System.Text.Json.JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new { content = new { parts = new[] { new { text } } } },
            },
        });
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    public static HttpResponseMessage GeminiError(System.Net.HttpStatusCode status) =>
        new(status) { Content = new StringContent("{\"error\":\"boom\"}") };

    public static (GeminiService Gemini, QueuedHttpMessageHandler Handler) BuildGemini(
        Dictionary<string, string?>? configOverrides = null,
        params HttpResponseMessage[] responses)
    {
        var handler = new QueuedHttpMessageHandler(responses);
        var httpClient = new HttpClient(handler);

        var configValues = configOverrides ?? new Dictionary<string, string?> { ["Gemini:ApiKey"] = "test-key" };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();

        var gemini = new GeminiService(httpClient, config, NullLogger<GeminiService>.Instance, new GeminiKeyRotator());
        return (gemini, handler);
    }

    public static TicketClassificationService BuildClassificationService(GeminiService gemini)
    {
        var knowledgeBase = new KnowledgeBaseService(new FakeWebHostEnvironment(), NullLogger<KnowledgeBaseService>.Instance);
        return new TicketClassificationService(gemini, knowledgeBase, NullLogger<TicketClassificationService>.Instance);
    }
}
