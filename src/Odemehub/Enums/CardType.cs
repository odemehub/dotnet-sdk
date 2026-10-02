namespace Odemehub.Enums;

/// <summary>
/// Whether the money on a card is lent, drawn from an account or loaded beforehand.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> stands for a value this version of the client does
/// not know yet: the gateway may grow a new one, and an answer carrying it is
/// still read rather than turned down. It is never sent.
/// </remarks>
public enum CardType
{
    Unknown = 0,
    Credit,
    Debit,
    Prepaid,
}
