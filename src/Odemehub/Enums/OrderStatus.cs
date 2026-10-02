namespace Odemehub.Enums;

/// <summary>
/// Where an order stands: open until it is paid, then paid.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> stands for a value this version of the client does
/// not know yet: the gateway may grow a new one, and an answer carrying it is
/// still read rather than turned down. It is never sent.
/// </remarks>
public enum OrderStatus
{
    Unknown = 0,
    Open,
    Paid,
}
