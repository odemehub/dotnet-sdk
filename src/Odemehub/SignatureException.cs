namespace Odemehub;

/// <summary>
/// The answer did not carry the signature it should have. It was not signed
/// with the secret this client holds, so it cannot be shown to have come from
/// the gateway and must not be acted on.
/// </summary>
public class SignatureException : OdemehubException
{
    public SignatureException(string message) : base(message)
    {
    }
}
