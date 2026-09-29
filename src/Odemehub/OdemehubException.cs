using System;

namespace Odemehub;

/// <summary>
/// Base class for everything this client throws, so a caller that does not
/// care which way a payment failed can catch one thing.
/// </summary>
public class OdemehubException : Exception
{
    public OdemehubException(string message) : base(message)
    {
    }

    public OdemehubException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
