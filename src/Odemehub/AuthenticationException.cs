namespace Odemehub;

/// <summary>
/// The gateway did not accept the credentials: either the API key is not the
/// one issued to the team in the address, or the request was not signed with
/// the matching secret.
/// </summary>
public class AuthenticationException : OdemehubException
{
    public AuthenticationException(string message) : base(message)
    {
    }
}
