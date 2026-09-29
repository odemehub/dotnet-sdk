using System;

namespace Odemehub;

/// <summary>
/// The gateway could not be reached at all. Whether the payment was made is
/// unknown; the payment record on the gateway says what actually happened.
/// </summary>
public class TransportException : OdemehubException
{
    public TransportException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
