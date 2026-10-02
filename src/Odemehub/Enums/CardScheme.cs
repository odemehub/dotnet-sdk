namespace Odemehub.Enums;

/// <summary>
/// The network a card belongs to.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> stands for a value this version of the client does
/// not know yet: the gateway may grow a new one, and an answer carrying it is
/// still read rather than turned down. It is never sent.
/// </remarks>
public enum CardScheme
{
    Unknown = 0,
    Visa,
    Mastercard,
    AmericanExpress,
    Troy,
    Discover,
    DinersClub,
    Jcb,
    Unionpay,
    Maestro,
}
