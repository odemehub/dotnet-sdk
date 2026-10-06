namespace Odemehub.Enums;

/// <summary>
/// Whether a payment link is paid in the one money the merchant set
/// (<see cref="Fixed"/>), or the payer picks one of those the link offers
/// (<see cref="Selectable"/>).
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> stands for a value this version of the client does
/// not know yet: the gateway may grow a new one, and an answer carrying it is
/// still read rather than turned down. It is never sent.
/// </remarks>
public enum CurrencyType
{
    Unknown = 0,
    Fixed,
    Selectable,
}
