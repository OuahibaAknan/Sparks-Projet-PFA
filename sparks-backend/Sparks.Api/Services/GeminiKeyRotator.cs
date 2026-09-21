namespace Sparks.Api.Services;

/// <summary>Tracks which configured Gemini API key to try first across requests, so once a key is rate-limited the rest of the app stops hammering it.</summary>
public class GeminiKeyRotator
{
    private int _index;

    public int CurrentIndex(int keyCount) => keyCount <= 0 ? 0 : ((_index % keyCount) + keyCount) % keyCount;

    public void Advance() => Interlocked.Increment(ref _index);
}
