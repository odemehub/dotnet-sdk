namespace Odemehub.Enums;

/// <summary>
/// How money went back: a cancellation of the whole before the provider settled it, or a refund after.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> stands for a value this version of the client does
/// not know yet: the gateway may grow a new one, and an answer carrying it is
/// still read rather than turned down. It is never sent.
/// </remarks>
public enum RefundType
{
    Unknown = 0,
    Cancel,
    Refund,
}
