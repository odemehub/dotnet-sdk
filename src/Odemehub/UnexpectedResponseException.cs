namespace Odemehub;

/// <summary>
/// The gateway answered with something that is neither an outcome nor a
/// refusal this client has a type of its own for: something that went wrong
/// on the gateway's side (500), or an answer that could not be read.
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
