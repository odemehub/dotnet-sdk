namespace Odemehub.Enums;

/// <summary>
/// Where a subscription stands. A merchant may only ever send <see cref="Cancelled"/>; the rest follow the payments.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> stands for a value this version of the client does
/// not know yet: the gateway may grow a new one, and an answer carrying it is
/// still read rather than turned down. It is never sent.
/// </remarks>
public enum SubscriptionStatus
{
    Unknown = 0,
    Pending,
    Active,
    PastDue,
    Cancelled,
    Completed,
}
