namespace Odemehub.Enums;

/// <summary>
/// What became of the money of a payment, which can move on to refunded long after the attempt is over.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> stands for a value this version of the client does
/// not know yet: the gateway may grow a new one, and an answer carrying it is
/// still read rather than turned down. It is never sent.
/// </remarks>
public enum PaymentStatus
{
    Unknown = 0,
    Unpaid,
    Paid,
    Cancelled,
    Refunded,
    PartiallyRefunded,
}
