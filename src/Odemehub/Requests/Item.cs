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

    /// <summary>The tax inside the price, as a percentage: "20" or "20.00". Left out, the line carries no tax.</summary>
    public string? TaxRate { get; init; }

    /// <summary>The merchant's own key for what is on the line, if it has one.</summary>
    public string? Reference { get; init; }

    /// <summary>The https address of the picture shown beside the line at checkout.</summary>
    public string? Image { get; init; }

    /// <summary>
    /// Whether the line is also kept on the team's product list: written there
    /// under its reference, or the product with that reference brought up to
    /// the line. A line kept so has to carry a reference.
    /// </summary>
    public bool? SaveAsProduct { get; init; }

    internal JsonObject ToBody()
    {
        return Fields.Said(
            ("reference", Reference),
            ("name", Name),
            ("image", Image),
            ("quantity", Quantity),
            ("unit_amount", UnitAmount),
            ("tax_rate", TaxRate),
            ("save_as_product", SaveAsProduct));
    }

    /// <summary>The lines as the body carries them; null for lines nobody gave.</summary>
    internal static JsonArray? ToBody(IReadOnlyList<Item>? items)
    {
        return items is null ? null : new JsonArray(items.Select(item => (JsonNode)item.ToBody()).ToArray());
    }
}

