namespace Odemehub;

/// <summary>
/// The credentials were fine but the team may not do this (HTTP 403): it has
/// an unpaid balance, or the module the endpoint belongs to is turned off.
/// The message says which. ödemehub turns modules on; get in touch to have
/// one opened.
/// </summary>
public class ForbiddenException : OdemehubException
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
