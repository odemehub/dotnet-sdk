using System.Collections.Generic;

namespace Odemehub;

/// <summary>
/// The request reached the gateway and was signed correctly, but its contents
/// were refused. No payment was attempted.
/// </summary>
public class ValidationException : OdemehubException
{
    public ValidationException(string message, IReadOnlyDictionary<string, IReadOnlyList<string>> errors) : base(message)
    {
        Errors = errors;
    }

    /// <summary>The refused fields, each with the reasons it was refused.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }
}
