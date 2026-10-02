namespace Odemehub;

/// <summary>
/// The gateway did not accept the credentials (HTTP 401): either the API key
/// is not the one issued to the team in the address, or the request was not
/// signed with the matching secret, or it was signed too long ago — the
/// clock of the machine sending it may be off by more than five minutes.
/// </summary>
public class AuthenticationException : OdemehubException
{
    public AuthenticationException(string message) : base(message)
    {
    }
}
