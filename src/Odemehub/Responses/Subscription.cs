using System.Collections.Generic;
using System.Text.Json;

namespace Odemehub.Responses;

/// <summary>
/// A subscription as it stands: what it is for, the period it is on and
/// whether that period has been paid for.
/// </summary>
public sealed record Subscription
{
    internal Subscription(JsonElement body)
    {
        var subscription = body.Field("subscription");

        Result = new Result(body);
        Token = Read.String(subscription.Field("token"));
        ChannelToken = Read.String(subscription.Field("channel_token"));
        ChannelReference = Read.String(subscription.Field("channel_reference"));
        Items = Read.List(subscription.Field("items"), item => new SubscriptionItem(item));
        Status = Read.String(subscription.Field("status"));
        Period = Read.String(subscription.Field("period"));
        Amount = Read.String(subscription.Field("amount"));
        Currency = Read.String(subscription.Field("currency"));
        StartsAt = Read.NonEmptyString(subscription.Field("starts_at"));
        EndsAt = Read.NonEmptyString(subscription.Field("ends_at"));
        PaidAt = Read.NonEmptyString(subscription.Field("paid_at"));
        CancelledAt = Read.NonEmptyString(subscription.Field("cancelled_at"));
        CheckoutUrl = Read.NonEmptyString(subscription.Field("checkout_url"));
        IsTest = Read.OptionalBool(subscription.Field("is_test"));
        CustomerChannelReference = Read.NonEmptyString(body.Field("customer").Field("channel_reference"));
    }

    public Result Result { get; }

    /// <summary>The subscription's token in the gateway; name it to ask after it later.</summary>
    public string Token { get; }

    /// <summary>The channel the subscription was opened on.</summary>
    public string ChannelToken { get; }

    /// <summary>The key the subscription is known by in the calling system.</summary>
    public string ChannelReference { get; }

    /// <summary>What is subscribed to.</summary>
    public IReadOnlyList<SubscriptionItem> Items { get; }

    /// <summary>Where it stands: pending, active, past_due or cancelled.</summary>
    public string Status { get; }

    /// <summary>How often a period comes round: monthly or yearly.</summary>
    public string Period { get; }

    /// <summary>What the period it is on costs, with the kurus behind a point.</summary>
    public string Amount { get; }

    public string Currency { get; }

    /// <summary>When the period it is on began, once it has been paid for.</summary>
    public string? StartsAt { get; }

    /// <summary>When the period it is on runs out, which is when the next is charged.</summary>
    public string? EndsAt { get; }

    /// <summary>When the period it is on was paid for, if it has been.</summary>
    public string? PaidAt { get; }

    /// <summary>The day it was called off on, if it has been.</summary>
    public string? CancelledAt { get; }

    /// <summary>Where the customer pays the period it is on, while that is still owed.</summary>
    public string? CheckoutUrl { get; }

    /// <summary>Whether it was paid for in the test environment; null until the first payment.</summary>
    public bool? IsTest { get; }

    /// <summary>The merchant's own key for the customer, answered when the subscription is opened.</summary>
    public string? CustomerChannelReference { get; }

    /// <summary>
    /// Whether the subscription is being paid for: a customer who has been
    /// through the checkout and whose card has not since been turned away.
    /// </summary>
    public bool IsActive => Status == "active";

    /// <summary>
    /// Whether the first period has yet to be paid for. A subscription stays
    /// here until the customer has been through the checkout.
    /// </summary>
    public bool IsPending => Status == "pending";

    /// <summary>
    /// Whether a period has been left unpaid: the card was tried and turned
    /// away every time, and the customer has been asked to pay it themselves
    /// at <see cref="CheckoutUrl"/>.
    /// </summary>
    public bool IsPastDue => Status == "past_due";

    /// <summary>
    /// Whether it is over. A subscription that has been called off but is
    /// still serving days that were paid for is not over yet — read
    /// <see cref="CancelledAt"/> for that.
    /// </summary>
    public bool IsCancelled => Status == "cancelled";
}

/// <summary>
/// One line of what a subscription is for.
/// </summary>
public sealed record SubscriptionItem
{
    internal SubscriptionItem(JsonElement item)
    {
        ChannelReference = Read.String(item.Field("channel_reference"));
        Name = Read.String(item.Field("name"));
        Quantity = Read.Int(item.Field("quantity"));
        UnitAmount = Read.String(item.Field("unit_amount"));
        TaxRate = Read.OptionalString(item.Field("tax_rate"));
    }

    /// <summary>The merchant's own key for the product.</summary>
    public string ChannelReference { get; }

    public string Name { get; }

    public int Quantity { get; }

    /// <summary>The price of one, as digits with the kurus behind a point.</summary>
    public string UnitAmount { get; }

    /// <summary>The tax included in the price, as a percentage.</summary>
    public string? TaxRate { get; }
}

/// <summary>
/// Word the gateway sent about a subscription: the state it has reached and
/// the subscription as it stands now.
/// </summary>
public sealed record SubscriptionWebhook
{
    internal SubscriptionWebhook(JsonElement body)
    {
        Event = Read.String(body.Field("event"));
        Subscription = new Subscription(body);
    }

    /// <summary>The state reached: active, past_due, cancelled or ended.</summary>
    public string Event { get; }

    /// <summary>The subscription as it stands now.</summary>
    public Subscription Subscription { get; }

    /// <summary>
    /// Whether the subscription is being paid for: the customer has just paid
    /// a period, whether the first or a later one.
    /// </summary>
    public bool IsActive => Event == "active";

    /// <summary>
    /// Whether a period was left unpaid. The card was tried and turned away
    /// every time, and the customer has been asked to pay it themselves at the
    /// subscription's checkout address.
    /// </summary>
    public bool IsPastDue => Event == "past_due";

    /// <summary>
    /// Whether the subscription has been called off. Nothing more will be
    /// charged, but the customer is served until the period ends.
    /// </summary>
    public bool IsCancelled => Event == "cancelled";

    /// <summary>
    /// Whether it is over: the days that were paid for have run out and the
    /// customer's access can be closed.
    /// </summary>
    public bool IsEnded => Event == "ended";
}
