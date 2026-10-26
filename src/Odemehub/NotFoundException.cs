namespace Odemehub;

/// <summary>
/// What the request named is not there (HTTP 404): no payment, order, payment
/// link, subscription, kept card or bank account of the team's under that
/// token, or no team at that address. Only the calls that name one record by
/// its token to change, remove, refund or cancel it are answered this way; a
/// question (<c>Retrieve*</c>, by token, reference or days) answers with an
/// empty list instead. Nothing was done. The message says what was looked for.
/// </summary>
public class NotFoundException : OdemehubException
{
    public NotFoundException(string message) : base(message)
    {
    }
}
