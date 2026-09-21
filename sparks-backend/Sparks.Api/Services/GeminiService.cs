using System.Net;
using System.Text.Json.Nodes;

namespace Sparks.Api.Services;

public class GeminiService
{
    private const string FallbackMessage = "Sorry, I couldn't reach the AI assistant right now. Please try again shortly.";

    private readonly HttpClient _http;
    private readonly ILogger<GeminiService> _logger;
    private readonly GeminiKeyRotator _rotator;
    private readonly IReadOnlyList<string> _apiKeys;
    private readonly string _model;

    public GeminiService(HttpClient http, IConfiguration config, ILogger<GeminiService> logger, GeminiKeyRotator rotator)
    {
        _http = http;
        _logger = logger;
        _rotator = rotator;
        _model = config["Gemini:Model"] ?? "gemini-flash-latest";

        var keys = config.GetSection("Gemini:ApiKeys").Get<string[]>() ?? [];
        if (keys.Length == 0 && !string.IsNullOrWhiteSpace(config["Gemini:ApiKey"]))
            keys = [config["Gemini:ApiKey"]!];
        _apiKeys = keys.Where(k => !string.IsNullOrWhiteSpace(k)).ToList();
    }

    /// <summary>Free-form conversational reply (used by the ticket chat assistant).</summary>
    public async Task<string> AskAsync(string systemInstruction, string question)
    {
        var payload = BuildPayload(systemInstruction, question, responseSchema: null);
        var (text, error) = await CallAsync(payload);
        return text ?? error ?? FallbackMessage;
    }

    /// <summary>Structured reply constrained to the given Gemini JSON schema. Returns the raw JSON text on success.</summary>
    public async Task<string?> AskJsonAsync(string systemInstruction, string prompt, JsonObject responseSchema)
    {
        var payload = BuildPayload(systemInstruction, prompt, responseSchema);
        var (text, _) = await CallAsync(payload);
        return text;
    }

    private static JsonObject BuildPayload(string systemInstruction, string userText, JsonObject? responseSchema)
    {
        var payload = new JsonObject
        {
            ["systemInstruction"] = new JsonObject
            {
                ["parts"] = new JsonArray(new JsonObject { ["text"] = systemInstruction }),
            },
            ["contents"] = new JsonArray(
                new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray(new JsonObject { ["text"] = userText }),
                }
            ),
        };

        if (responseSchema is not null)
        {
            payload["generationConfig"] = new JsonObject
            {
                ["responseMimeType"] = "application/json",
                ["responseSchema"] = responseSchema,
            };
        }

        return payload;
    }

    /// <summary>Sends the payload, rotating across configured keys on quota/overload errors. Returns (text, errorMessage) — exactly one is non-null on completion.</summary>
    private async Task<(string? Text, string? Error)> CallAsync(JsonObject payload)
    {
        if (_apiKeys.Count == 0)
        {
            _logger.LogWarning("No Gemini API key configured.");
            return (null, "AI assistant is not configured (missing Gemini API key).");
        }

        var startIndex = _rotator.CurrentIndex(_apiKeys.Count);

        for (var attempt = 0; attempt < _apiKeys.Count; attempt++)
        {
            var keyIndex = (startIndex + attempt) % _apiKeys.Count;
            var apiKey = _apiKeys[keyIndex];
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={apiKey}";

            try
            {
                using var response = await _http.PostAsJsonAsync(url, payload);
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var root = JsonNode.Parse(body);
                    var text = root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
                    return string.IsNullOrWhiteSpace(text)
                        ? (null, "The AI assistant didn't return a response. Please try rephrasing your question.")
                        : (text.Trim(), null);
                }

                var isQuotaOrOverload = response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable;
                _logger.LogWarning("Gemini API error on key #{KeyIndex} ({Status}): {Body}", keyIndex, response.StatusCode, body);

                if (isQuotaOrOverload && attempt < _apiKeys.Count - 1)
                {
                    _rotator.Advance();
                    continue; // try the next key immediately
                }

                return (null, FallbackMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini API call failed on key #{KeyIndex}.", keyIndex);
                if (attempt == _apiKeys.Count - 1)
                    return (null, FallbackMessage);
            }
        }

        return (null, FallbackMessage);
    }
}
