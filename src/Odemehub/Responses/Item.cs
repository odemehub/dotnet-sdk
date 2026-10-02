using System.Text.Json;

namespace Odemehub.Responses;

/// <summary>
/// One line of what an order, a subscription or a payment link is for, as it
/// was written down.
/// </summary>
public sealed record Item
{
    internal Item(JsonElement item)
    {
        ChannelReference = Read.NonEmptyString(item.Field("channel_reference"));
        Name = Read.String(item.Field("name"));
        Image = Read.NonEmptyString(item.Field("image"));
        Quantity = Read.Int(item.Field("quantity"));
        UnitAmount = Read.String(item.Field("unit_amount"));
        TaxRate = Read.String(item.Field("tax_rate"));
    }

    /// <summary>The merchant's own key for what is on the line; null when it gave none.</summary>
    public string? ChannelReference { get; }

    public string Name { get; }

    /// <summary>The picture the line is shown with; null when it has none.</summary>
    public string? Image { get; }

    public int Quantity { get; }

    /// <summary>The price of one, tax included, as digits with the kurus behind a point.</summary>
    public string UnitAmount { get; }

    /// <summary>The tax included in the price, as a percentage.</summary>
    public string TaxRate { get; }
}

/// <summary>
/// One way the goods of an order or a subscription may be sent.
/// </summary>
public sealed record ShippingMethod
{
    internal ShippingMethod(JsonElement method)
    {
        Handle = Read.String(method.Field("handle"));
        Title = Read.String(method.Field("title"));
        Amount = Read.String(method.Field("amount"));
        TaxRate = Read.String(method.Field("tax_rate"));
    }

    /// <summary>The merchant's own key for it.</summary>
    public string Handle { get; }

    /// <summary>What the payer sees.</summary>
    public string Title { get; }

    /// <summary>What it costs, tax included.</summary>
    public string Amount { get; }

    /// <summary>The tax inside the amount, as a percentage.</summary>
    public string TaxRate { get; }
}
