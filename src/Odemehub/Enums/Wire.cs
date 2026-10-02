using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Odemehub.Enums;

/// <summary>
/// How the values of the client's enums are written on the wire: the
/// snake_case the gateway speaks (<c>PastDue</c> is <c>past_due</c>), and a name
/// written in capitals as it is (<c>TRY</c>).
/// </summary>
internal static class Wire
{
    /// <summary>
    /// The value as the gateway writes it; null for <c>Unknown</c>, which is
    /// never sent.
    /// </summary>
    internal static string? Of<T>(T? value) where T : struct, Enum
    {
        return value is null || Convert.ToInt32(value.Value) == 0 ? null : Values<T>.ToWire[value.Value];
    }

    /// <summary>
    /// The member a value the gateway wrote stands for: <c>Unknown</c> for one
    /// this version does not know, and null for one that was not there.
    /// </summary>
    internal static T? Read<T>(string? value) where T : struct, Enum
    {
        if (value is null)
        {
            return null;
        }

        return Values<T>.FromWire.TryGetValue(value, out var member) ? member : default(T);
    }

    private static class Values<T> where T : struct, Enum
    {
        internal static readonly Dictionary<T, string> ToWire = Enum.GetValues<T>().ToDictionary(member => member, member => Name(member.ToString()));

        internal static readonly Dictionary<string, T> FromWire = ToWire.Where(pair => Convert.ToInt32(pair.Key) != 0).ToDictionary(pair => pair.Value, pair => pair.Key);
    }

    private static string Name(string member)
    {
        if (member.All(letter => !char.IsLower(letter)))
        {
            return member;
        }

        var name = new StringBuilder();

        foreach (var letter in member)
        {
            if (char.IsUpper(letter) && name.Length > 0)
            {
                name.Append('_');
            }

            name.Append(char.ToLowerInvariant(letter));
        }

        return name.ToString();
    }
}
