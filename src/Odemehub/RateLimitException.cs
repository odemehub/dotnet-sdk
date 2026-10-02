namespace Odemehub;

/// <summary>
/// The team sent more requests in a minute than the gateway takes (HTTP 429):
/// 300 across every endpoint, 60 for the ones that move money or keep a card.
/// Nothing was done; the same request can be sent again once the minute is
/// over.
/// </summary>
public class RateLimitException : OdemehubException
{
    public RateLimitException(string message, int? retryAfter) : base(message)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>How many seconds to wait before trying again, as the gateway said it in <c>Retry-After</c>; null when it did not say.</summary>
    public int? RetryAfter { get; }
}
