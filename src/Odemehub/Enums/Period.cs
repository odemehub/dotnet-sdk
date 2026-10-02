namespace Odemehub.Enums;

/// <summary>
/// How often a subscription renews.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> stands for a value this version of the client does
/// not know yet: the gateway may grow a new one, and an answer carrying it is
/// still read rather than turned down. It is never sent.
/// </remarks>
public enum Period
{
    Unknown = 0,
    Daily,
    Weekly,
    Monthly,
    Annually,
}
