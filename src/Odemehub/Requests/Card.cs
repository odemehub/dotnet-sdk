using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// The card a payment is attempted with, or kept without one. The number and
/// the security code travel no further than the request body, and are kept
/// out of <see cref="ToString"/> so they never reach a log: the gateway keeps
/// only the head and the tail digits of the number and no digit of the code.
/// </summary>
public sealed class Card
{
    public required string HolderName { get; init; }

    /// <summary>The number, 12 to 19 digits; it may be written in groups with spaces.</summary>
    public required string Number { get; init; }

    /// <summary>
    /// Three or four digits. A payment always needs it. A card kept without a
    /// payment needs it only at providers that keep a card by charging and
    /// giving back a small amount; left out, it is not sent.
    /// </summary>
    public string? SecurityCode { get; init; }

    /// <summary>Two digits, e.g. 04.</summary>
    public required string ExpiryMonth { get; init; }

    /// <summary>Four digits, e.g. 2030.</summary>
    public required string ExpiryYear { get; init; }

    /// <summary>
    /// Whether the customer asked for this card to be kept after a successful
    /// payment, so they can pay with it again without typing it out. Needs a
    /// customer reference to be kept under, a plan that covers saved cards and
    /// an account whose provider keeps cards; one that cannot turns the
    /// payment down on this field rather than declining it. Read only by
    /// payments; left out, it is not sent.
    /// </summary>
    public bool? ShouldSave { get; init; }

    internal JsonObject ToBody()
    {
        return Fields.Said(
            ("holder_name", HolderName),
            ("number", Number),
            ("security_code", SecurityCode),
            ("expiry_month", ExpiryMonth),
            ("expiry_year", ExpiryYear),
            ("should_save", ShouldSave));
    }

    public override string ToString()
    {
        return $"Card {{ HolderName = {HolderName}, Number = *****, SecurityCode = *****, ExpiryMonth = {ExpiryMonth}, ExpiryYear = {ExpiryYear}, ShouldSave = {ShouldSave} }}";
    }
}
