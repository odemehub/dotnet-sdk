using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// One line of what an order, a subscription or a payment link is for. The
/// unit price includes the tax: a line of 120 at 20% is 100 of goods and 20 of
/// tax, and the gateway splits it so. Nothing is looked up in a catalogue;
/// what is sent is what is sold. What the whole comes to is never sent: the
/// gateway adds the lines up and answers with the total.
/// </summary>
public sealed class Item
{
    public required string Name { get; init; }

    /// <summary>The price of one, tax included, as digits with the kurus behind a point: "120.00".</summary>
    public required string UnitAmount { get; init; }

    /// <summary>1 to 9999.</summary>
    public required int Quantity { get; init; }

    /// <summary>The tax inside the price, as a percentage: "20" or "20.00".</summary>
    public required string TaxRate { get; init; }

    /// <summary>The merchant's own key for what is on the line, if it has one.</summary>
    public string? ChannelReference { get; init; }

    /// <summary>The https address of the picture shown beside the line at checkout.</summary>
    public string? Image { get; init; }

    internal JsonObject ToBody()
    {
        return Fields.Said(
            ("channel_reference", ChannelReference),
            ("name", Name),
            ("image", Image),
            ("quantity", Quantity),
            ("unit_amount", UnitAmount),
            ("tax_rate", TaxRate));
    }

    /// <summary>The lines as the body carries them; null for lines nobody gave.</summary>
    internal static JsonArray? ToBody(IReadOnlyList<Item>? items)
    {
        return items is null ? null : new JsonArray(items.Select(item => (JsonNode)item.ToBody()).ToArray());
    }
}

/// <summary>
/// One way the goods of an order or a subscription may be sent, offered to
/// the payer on the checkout page. The one they pick is added to what they
/// pay. The handle is the merchant's own key for it and has to be unique
/// within the list; the amount includes the tax, like a line's price.
/// </summary>
public sealed class ShippingMethod
{
    public required string Handle { get; init; }

    /// <summary>What the payer sees, e.g. "Standart Kargo".</summary>
    public required string Title { get; init; }

    /// <summary>What it costs, tax included, as digits with the kurus behind a point; "0" for free.</summary>
    public required string Amount { get; init; }

    /// <summary>The tax inside the amount, as a percentage.</summary>
    public required string TaxRate { get; init; }

    internal JsonObject ToBody()
    {
        return Fields.Of(
            ("handle", Handle),
            ("title", Title),
            ("amount", Amount),
            ("tax_rate", TaxRate));
    }

    /// <summary>The ways as the body carries them; null for ways nobody gave.</summary>
    internal static JsonArray? ToBody(IReadOnlyList<ShippingMethod>? methods)
    {
        return methods is null ? null : new JsonArray(methods.Select(method => (JsonNode)method.ToBody()).ToArray());
    }
}
