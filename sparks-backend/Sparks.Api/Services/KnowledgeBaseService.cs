using System.Text.Json;

namespace Sparks.Api.Services;

public record KnowledgeBaseEntry(
    int Id,
    string Title,
    string Category,
    string Priority,
    string Status,
    string Content,
    string Solution,
    List<string>? Keywords
);

/// <summary>Loads the static support knowledge base (Data/Knowledge/knowledge_base.json) and does simple keyword search to ground AI chat answers.</summary>
public class KnowledgeBaseService
{
    private static readonly char[] Separators = [' ', ',', '.', '?', '!', ':', ';', '\n', '\r', '\t', '-', '/', '(', ')'];

    private readonly List<KnowledgeBaseEntry> _entries;
    private readonly ILogger<KnowledgeBaseService> _logger;

    public KnowledgeBaseService(IWebHostEnvironment env, ILogger<KnowledgeBaseService> logger)
    {
        _logger = logger;
        var path = Path.Combine(env.ContentRootPath, "Data", "Knowledge", "knowledge_base.json");

        if (!File.Exists(path))
        {
            _logger.LogWarning("Knowledge base file not found at {Path}", path);
            _entries = [];
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            _entries = JsonSerializer.Deserialize<List<KnowledgeBaseEntry>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            _logger.LogInformation("Loaded {Count} knowledge base entries from {Path}", _entries.Count, path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load knowledge base from {Path}", path);
            _entries = [];
        }
    }

    public IReadOnlyList<KnowledgeBaseEntry> Search(string query, int topN = 3)
    {
        if (_entries.Count == 0 || string.IsNullOrWhiteSpace(query)) return [];

        var terms = query.ToLowerInvariant()
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .ToHashSet();

        if (terms.Count == 0) return [];

        return _entries
            .Select(e => (Entry: e, Score: Score(e, terms)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topN)
            .Select(x => x.Entry)
            .ToList();
    }

    private static int Score(KnowledgeBaseEntry entry, HashSet<string> terms)
    {
        var score = 0;
        var haystack = $"{entry.Title} {entry.Category} {entry.Content}".ToLowerInvariant();

        foreach (var term in terms)
        {
            if (haystack.Contains(term)) score++;
        }

        if (entry.Keywords is not null)
        {
            foreach (var keyword in entry.Keywords)
            {
                if (terms.Contains(keyword.ToLowerInvariant())) score += 2;
            }
        }

        return score;
    }
}
