namespace Odemehub;

/// <summary>
/// The gateway answered with something that is neither a payment outcome nor
/// a refusal this client knows how to read.
/// </summary>
public class UnexpectedResponseException : OdemehubException
{
    public UnexpectedResponseException(string message, int status) : base(message)
    {
        Status = status;
    }

    /// <summary>The HTTP status the gateway answered with.</summary>
    public int Status { get; }
}
