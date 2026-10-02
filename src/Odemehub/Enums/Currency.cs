namespace Odemehub.Enums;

/// <summary>
/// The money a payment, an order, a subscription or a payment link is taken in.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> stands for a value this version of the client does
/// not know yet: the gateway may grow a new one, and an answer carrying it is
/// still read rather than turned down. It is never sent.
/// </remarks>
public enum Currency
{
    Unknown = 0,
    TRY,
    USD,
    EUR,
    GBP,
}
