using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// The most installments the payer is offered on the checkout page, said apart
/// for cards issued to people (<see cref="Individual"/>) and to companies
/// (<see cref="Commercial"/>): each 1 to 12, where 1 is a single payment only.
/// A kind of card left without a number has no limit.
/// </summary>
/// <remarks>
/// Sent on a change, it takes the place of the limits there were; left out,
/// they stay. One with neither number set lifts every limit.
/// </remarks>
public sealed class MaxInstallments
{
    /// <summary>The most installments on a card issued to a person, 1 to 12; null for no limit.</summary>
    public int? Individual { get; init; }

    /// <summary>The most installments on a card issued to a company, 1 to 12; null for no limit.</summary>
    public int? Commercial { get; init; }

    /// <summary>
    /// Both kinds of card, a number or not: a limit left unset goes as nothing,
    /// which is how a change lifts it.
    /// </summary>
    internal JsonObject ToBody()
    {
        return Fields.Of(
            ("individual", Individual),
            ("commercial", Commercial));
    }
}
