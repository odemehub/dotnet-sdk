using System.Text.Json;

namespace Odemehub.Responses;

/// <summary>
/// The most installments the payer is offered on the checkout page, said apart
/// for cards issued to people and to companies; 1 is a single payment only.
/// </summary>
public sealed record MaxInstallments
{
    internal MaxInstallments(JsonElement limits)
    {
        Individual = Read.OptionalInt(limits.Field("individual"));
        Commercial = Read.OptionalInt(limits.Field("commercial"));
    }

    /// <summary>The most installments on a card issued to a person; null for no limit.</summary>
    public int? Individual { get; }

    /// <summary>The most installments on a card issued to a company; null for no limit.</summary>
    public int? Commercial { get; }

    /// <summary>The limits a thing carries; null for one that has none.</summary>
    internal static MaxInstallments? Of(JsonElement limits)
    {
        return limits.ValueKind == JsonValueKind.Object ? new MaxInstallments(limits) : null;
    }
}
