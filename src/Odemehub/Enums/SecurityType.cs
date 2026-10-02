namespace Odemehub.Enums;

/// <summary>
/// How a payment was made: confirmed at the bank (3D) or charged straight to the card.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> stands for a value this version of the client does
/// not know yet: the gateway may grow a new one, and an answer carrying it is
/// still read rather than turned down. It is never sent.
/// </remarks>
public enum SecurityType
{
    Unknown = 0,
    Secure,
    Regular,
}
