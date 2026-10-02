namespace Odemehub;

/// <summary>
/// The credentials were fine but the team may not do this (HTTP 403): it has
/// an unpaid balance, its plan could not be charged or has run out, or its
/// plan does not include the feature the endpoint belongs to. The message
/// says which.
/// </summary>
public class ForbiddenException : OdemehubException
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
