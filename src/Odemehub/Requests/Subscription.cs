using System.Collections.Generic;
using System.Text.Json.Nodes;
using Odemehub.Enums;

namespace Odemehub.Requests;

/// <summary>
/// A subscription opened for a customer, its first renewal to be paid on the
/// gateway's own checkout page and the rest taken from the card kept then.
/// The answer carries the checkout address; the customer is sent there, pays
/// with a card the gateway keeps as their default, and is posted back to
/// <see cref="SuccessUrl"/>. The addresses set for its channel under Webhook
/// in the panel hear every change of state after that.
/// </summary>
/// <remarks>
/// The customer needs a reference, because that is what the card the renewals
/// are taken from is kept under. The account, named or default, has to keep
/// cards and take 3D payments, and the plan has to cover saved cards.
/// </remarks>
public sealed class CreateSubscription : CheckoutMessage
{
    /// <summary>The key the subscription is known by in the calling system. Has to carry at least one digit.</summary>
    public required string ChannelReference { get; init; }

    /// <summary>How often a renewal comes round.</summary>
    public required Period Period { get; init; }

    /// <summary>
    /// Where the customer's browser is posted back to once the first renewal
    /// is paid, with the payment's token. An address reachable from the
    /// internet.
    /// </summary>
    public required string SuccessUrl { get; init; }

    /// <summary>What is subscribed to; at least one line, at most a hundred.</summary>
    public required IReadOnlyList<Item> Items { get; init; }

    /// <summary>Who is subscribing; the reference is required, the rest is asked on the checkout page if left out.</summary>
    public required Customer Customer { get; init; }

    /// <summary>How many renewals are paid in all, 1 to 1000, after which it is completed. Left out, it runs until it is called off.</summary>
    public int? RenewalLimit { get; init; }

    internal override string Path => "create-subscription";

    internal override JsonObject ToBody(string channelToken)
    {
        var details = Details(Channel(channelToken), ChannelReference, SuccessUrl, Items);
        details["period"] = Wire.Of<Period>(Period);

        if (RenewalLimit is not null)
        {
            details["renewal_limit"] = RenewalLimit;
        }

        return Body("subscription", details, Customer);
    }
}

/// <summary>
/// Where a subscription stands, by its token: what it is for, the renewal it
/// is on and whether that has been paid for.
/// </summary>
public sealed class RetrieveSubscription : RetrieveByToken
{
    internal override string Endpoint => "retrieve-subscription";
}

/// <summary>
/// Where the latest subscription under one of the merchant's own keys on a
/// channel stands.
/// </summary>
public sealed class RetrieveSubscriptionByReference : RetrieveByReference
{
    internal override string Path => "retrieve-subscription-by-reference";
}

/// <summary>
/// Every subscription opened on a channel within a span of days, oldest
/// first, each with whose it is.
/// </summary>
public sealed class RetrieveSubscriptionsByChannelReference : RetrieveByChannelReference
{
    internal override string Path => "retrieve-subscriptions-by-channel-reference";
}

/// <summary>
/// A change to a subscription, named by its token in the address and again in
/// the body. Only what is sent is written: lines sent replace the lines there
/// were and reach the renewal still owed and every one after it, and the
/// customer sent is written over the one there was.
/// </summary>
/// <remarks>
/// This is also how a subscription is called off: send the status
/// <see cref="SubscriptionStatus.Cancelled"/>, the one status a merchant may set. Nothing is charged
/// after that and nothing is given back; a renewal already paid is served to
/// its end.
///
/// Until its first payment anything about it may be changed. Once paid, what
/// it renews on stays as it was opened — the channel, the customer's
/// reference, the account, the currency and the period — and the gateway
/// turns down a change to any of them (422). A renewal limit may not fall
/// below the renewals already paid.
///
/// The channel is written only when this message names one; the client's own
/// is not sent.
/// </remarks>
public sealed class UpdateSubscription : CheckoutMessage
{
    /// <summary>The subscription's token in the gateway.</summary>
    public required string Token { get; init; }

    /// <summary>Only <see cref="SubscriptionStatus.Cancelled"/> is accepted; the other states follow the payments.</summary>
    public SubscriptionStatus? Status { get; init; }

    public Period? Period { get; init; }

    /// <summary>1 to 1000, and never fewer than the renewals already paid.</summary>
    public int? RenewalLimit { get; init; }

    public string? ChannelReference { get; init; }

    public string? SuccessUrl { get; init; }

    /// <summary>Sent, they replace every line there was.</summary>
    public IReadOnlyList<Item>? Items { get; init; }

    public Customer? Customer { get; init; }

    /// <summary>
    /// Fields to set to nothing, by their names in the body:
    /// <c>renewal_limit</c> (run until called off), <c>description</c>,
    /// <c>cancel_url</c>, <c>payment_provider_token</c>.
    /// </summary>
    public IReadOnlyList<string>? Clear { get; init; }

    internal override string Path => $"update-subscription/{Token}";

    internal override JsonObject ToBody(string channelToken)
    {
        var details = Details(ChannelToken, ChannelReference, SuccessUrl, Items);

        foreach (var (key, value) in new (string, JsonNode?)[] { ("period", Wire.Of(Period)), ("renewal_limit", RenewalLimit), ("status", Wire.Of(Status)) })
        {
            if (value is not null)
            {
                details[key] = value;
            }
        }

        return Body("subscription", Cleared(details, Clear), Customer, Token);
    }
}
