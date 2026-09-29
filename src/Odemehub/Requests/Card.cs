using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// The card a payment is attempted with. The number and the security code
/// travel no further than the request body, and are kept out of
/// <see cref="ToString"/> so they never reach a log: the gateway keeps only the
/// head and the tail digits of the number and no digit of the code.
/// </summary>
public sealed class Card
{
    public required string HolderName { get; init; }

    /// <summary>The number, digits only, without spaces.</summary>
    public required string Number { get; init; }

    /// <summary>
    /// The security code. Only a card kept with <c>SaveCardAsync</c> at a
    /// provider with a card store of its own may leave it out.
    /// </summary>
    public string? SecurityCode { get; init; }

    /// <summary>Two digits, e.g. 04.</summary>
    public required string ExpiryMonth { get; init; }

    /// <summary>Four digits, e.g. 2030.</summary>
    public required string ExpiryYear { get; init; }

    /// <summary>
    /// Whether the customer asked for this card to be kept, so they can pay
    /// with it again without typing it out. The account's provider has to be
    /// able to charge a kept card; one that cannot turns the payment down on
    /// this field rather than declining it.
    /// </summary>
    public bool ShouldSave { get; init; }

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
