namespace Odemehub.Enums;

/// <summary>
/// Where a payment attempt got to. <see cref="Timeout"/> is an attempt the provider never answered.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> stands for a value this version of the client does
/// not know yet: the gateway may grow a new one, and an answer carrying it is
/// still read rather than turned down. It is never sent.
/// </remarks>
public enum TransactionStatus
{
    Unknown = 0,
    Started,
    RedirectedToSecurePage,
    ReturnedFromSecurePage,
    Timeout,
    Failed,
    Expired,
    Successful,
}
