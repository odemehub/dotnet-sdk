namespace Odemehub;

/// <summary>
/// What the request named is not there (HTTP 404): no payment, order, payment
/// link, subscription or kept card of the team's under that token, or no team
/// at that address. Nothing was done. The message says what was looked for.
/// </summary>
public class NotFoundException : OdemehubException
{
    public NotFoundException(string message) : base(message)
    {
    }
}
