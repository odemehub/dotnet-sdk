using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// A subscription opened for a customer and paid for the first time on the
/// gateway's own page. Nothing is charged here: the answer carries the address
/// to send the customer to. The card is kept, because the periods to come are
/// taken from it.
/// </summary>
/// <remarks>
/// The products subscribed to come round at the same frequency and are priced
/// in the same money, because a subscription is charged as one thing.
/// </remarks>
public sealed class SubscriptionPayment : ChannelMessage
{
    /// <summary>The key the subscription is known by in the calling system.</summary>
    public required string ChannelReference { get; init; }

    /// <summary>What is subscribed to; at least one line, each product once.</summary>
    public required IReadOnlyList<SubscriptionItem> Items { get; init; }

    /// <summary>Where the customer is posted back to, with the signed outcome, once the first period is paid.</summary>
    public required string SuccessUrl { get; init; }

    public required Customer Customer { get; init; }

    /// <summary>Where the customer goes if they turn back without paying.</summary>
    public string? CancelUrl { get; init; }

    /// <summary>Where the merchant is told, signed, whenever the subscription's state changes.</summary>
    public string? WebhookUrl { get; init; }

    /// <summary>
    /// The payment account the subscription is paid through, by its token; the
    /// card is kept there and renewals are taken there. Left out, the
    /// merchant's Gate rules pick the account, and its default account is used
    /// where none of them holds.
    /// </summary>
    public string? PaymentProviderToken { get; init; }

    internal override string Path => "subscription-payment";

    internal override JsonObject ToBody(string channelToken)
    {
        return Fields.Of(
            ("subscription", Fields.Said(
                ("channel_token", Channel(channelToken)),
                ("channel_reference", ChannelReference),
                ("payment_provider_token", PaymentProviderToken),
                ("items", new JsonArray(Items.Select(item => (JsonNode)item.ToBody()).ToArray())),
                ("success_url", SuccessUrl),
                ("cancel_url", CancelUrl),
                ("webhook_url", WebhookUrl))),
            ("customer", Customer.ToBody()));
    }
}

/// <summary>
/// One line of what a subscription is for: one of the merchant's recurring
/// products, named by its own key for it. What it costs and how often it comes
/// round are the product's, as saved with <c>SaveProductAsync</c>.
/// </summary>
public sealed class SubscriptionItem
{
    /// <summary>The key the recurring product is saved under on the subscription's channel.</summary>
    public required string ChannelReference { get; init; }

    /// <summary>Left out, the line is for one.</summary>
    public int? Quantity { get; init; }

    /// <summary>
    /// The price of one for the first period only, as digits with the kurus
    /// behind a point: an opening offer. The periods after it are charged at
    /// the product's own price. Left out, the first period is charged at that
    /// price too.
    /// </summary>
    public string? UnitAmount { get; init; }

    internal JsonObject ToBody()
    {
        return Fields.Said(
            ("channel_reference", ChannelReference),
            ("quantity", Quantity),
            ("unit_amount", UnitAmount));
    }
}

/// <summary>
/// Where a subscription stands: what it is for, the period it is on and
/// whether that period has been paid for. Nothing is changed by asking.
/// </summary>
public sealed class RetrieveSubscription : SubscriptionMessage
{
    internal override string Path => "retrieve-subscription";
}

/// <summary>
/// A subscription called off. Nothing is given back: the customer keeps the
/// days they have already paid for and is served to the end of them, and
/// nothing is charged after that.
/// </summary>
public sealed class CancelSubscription : SubscriptionMessage
{
    internal override string Path => "cancel-subscription";
}
