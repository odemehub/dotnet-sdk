using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// An order opened to be paid on the gateway's own page. Nothing is charged
/// here: the answer carries the address to send the customer to, and they
/// give their card there. The customer is given whole, so the page asks for
/// nothing but the card.
/// </summary>
/// <remarks>
/// What the order comes to is not sent. The gateway adds up the lines and
/// answers with the amount, so the total can never disagree with what it is
/// made up of.
/// </remarks>
public sealed class OrderPayment : ChannelMessage
{
    /// <summary>The number the order is known by in the calling system.</summary>
    public required string ChannelReference { get; init; }

    /// <summary>Where the customer is posted back to, with the signed outcome, once the order is paid.</summary>
    public required string SuccessUrl { get; init; }

    public required Customer Customer { get; init; }

    /// <summary>What the order is made up of; at least one line.</summary>
    public required IReadOnlyList<OrderItem> Items { get; init; }

    /// <summary>Where the customer goes if they turn back without paying.</summary>
    public string? CancelUrl { get; init; }

    public string? Description { get; init; }

    /// <summary>Three letters, e.g. TRY. Left out, the gateway takes the lira.</summary>
    public string? Currency { get; init; }

    /// <summary>
    /// The payment account the order is paid through, by its token. Left out,
    /// the merchant's Gate rules pick the account, and its default account is
    /// used where none of them holds.
    /// </summary>
    public string? PaymentProviderToken { get; init; }

    internal override string Path => "order-payment";

    internal override JsonObject ToBody(string channelToken)
    {
        return Fields.Of(
            ("order", Fields.Said(
                ("channel_token", Channel(channelToken)),
                ("channel_reference", ChannelReference),
                ("payment_provider_token", PaymentProviderToken),
                ("description", Description),
                ("currency", Currency),
                ("success_url", SuccessUrl),
                ("cancel_url", CancelUrl),
                ("items", new JsonArray(Items.Select(item => (JsonNode)item.ToBody()).ToArray())))),
            ("customer", Customer.ToBody()));
    }
}

/// <summary>
/// One line of what an order is made up of. A line names one of the
/// merchant's products by its own key for it; whatever it leaves unsaid —
/// name, price, tax — is filled in from the product saved with
/// <c>SaveProductAsync</c>. What it does say holds for this order alone; the
/// product itself is never changed by an order.
/// </summary>
/// <remarks>
/// A line whose key names no product still goes through, as long as it brings
/// its own name and price: something sold once and never again does not have
/// to be saved as a product first.
/// </remarks>
public sealed class OrderItem
{
    /// <summary>The key the product is saved under on the order's channel.</summary>
    public required string ChannelReference { get; init; }

    /// <summary>Left out, the product's own name is shown.</summary>
    public string? Name { get; init; }

    /// <summary>The https address of the picture shown beside the line at checkout. Left out, the product's own picture is shown.</summary>
    public string? Image { get; init; }

    /// <summary>Left out, the line is for one.</summary>
    public int? Quantity { get; init; }

    /// <summary>The price of one, as digits with the kurus behind a point. Left out, the product's own price is charged.</summary>
    public string? UnitAmount { get; init; }

    /// <summary>The tax included in the price, as a percentage, e.g. "20". Left out, the product's own rate is used.</summary>
    public string? TaxRate { get; init; }

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
}

/// <summary>
/// A product written down in the merchant's catalogue at the gateway, under
/// the merchant's own key for it on one of its channels. Order lines and
/// subscriptions name products by that key.
/// </summary>
/// <remarks>
/// The same key on the same channel is the same product: sending it again
/// changes the one already saved rather than saving a second, so a merchant
/// can keep its own catalogue in step by sending every change as it happens.
/// A product is never deleted; it is taken off sale with
/// <c>IsActive = false</c>.
/// </remarks>
public sealed class SaveProduct : ChannelMessage
{
    /// <summary>The key the product is known by in the calling system.</summary>
    public required string ChannelReference { get; init; }

    public required string Name { get; init; }

    /// <summary>"simple" for something sold once, "recurring" for something subscribed to.</summary>
    public required string Type { get; init; }

    /// <summary>The price of one, as digits with the kurus behind a point.</summary>
    public required string Amount { get; init; }

    /// <summary>The tax included in the price, as a percentage, e.g. "20".</summary>
    public required string TaxRate { get; init; }

    /// <summary>How often a recurring product comes round: "monthly" or "annually". Only a recurring product has one.</summary>
    public string? Period { get; init; }

    /// <summary>Three letters, e.g. TRY. Left out, the gateway takes the lira.</summary>
    public string? Currency { get; init; }

    /// <summary>Whether it is on sale. Left out, it is.</summary>
    public bool? IsActive { get; init; }

    /// <summary>
    /// The https address of the picture the checkout shows it with. Left out,
    /// the product keeps the picture it has; an empty string takes it off.
    /// </summary>
    public string? Image { get; init; }

    internal override string Path => "save-product";

    internal override JsonObject ToBody(string channelToken)
    {
        return Fields.Of(
            ("product", Fields.Said(
                ("channel_token", Channel(channelToken)),
                ("channel_reference", ChannelReference),
                ("name", Name),
                ("image", Image),
                ("type", Type),
                ("amount", Amount),
                ("currency", Currency),
                ("tax_rate", TaxRate),
                ("period", Period),
                ("is_active", IsActive))));
    }
}
