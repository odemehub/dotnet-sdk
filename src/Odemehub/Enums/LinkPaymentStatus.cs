namespace Odemehub.Enums;

/// <summary>
/// Where a payment at a payment link stands: opened as the payer pays, open
/// while their bank turns them away, and paid once a payment goes through.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> stands for a value this version of the client does
/// not know yet: the gateway may grow a new one, and an answer carrying it is
/// still read rather than turned down. It is never sent.
/// </remarks>
public enum LinkPaymentStatus
{
    Unknown = 0,
    Open,
    Paid,
}
