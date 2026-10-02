using System.Collections.Generic;
using System.Text.Json;
using Odemehub.Enums;

namespace Odemehub.Responses;

/// <summary>
/// A subscription as it stands: what it is written as — the way an order is —
/// how often it renews, where it stands, the renewal it is on, when the next
/// one falls due, whose it is and the address a renewal still owed is paid at.
/// </summary>
public sealed record Subscription
{
    /// <summary>A subscription as an answer carries it: under <c>subscription</c>, with the customer beside it.</summary>
    internal Subscription(JsonElement body) : this(body.Field("subscription"), body.Field("customer"))
    {
    }

    internal Subscription(JsonElement subscription, JsonElement customer)
    {
        var shippingMethod = subscription.Field("shipping_method");

        Token = Read.String(subscription.Field("token"));
        ChannelToken = Read.String(subscription.Field("channel_token"));
        ChannelReference = Read.String(subscription.Field("channel_reference"));
        Description = Read.NonEmptyString(subscription.Field("description"));
        PaymentProviderToken = Read.NonEmptyString(subscription.Field("payment_provider_token"));
        Status = Read.Enum<SubscriptionStatus>(subscription.Field("status"));
        Period = Read.Enum<Period>(subscription.Field("period"));
        RenewalLimit = Read.OptionalInt(subscription.Field("renewal_limit"));
        RenewalsPaid = Read.Int(subscription.Field("renewals_paid"));
        Items = Read.List(subscription.Field("items"), item => new Item(item));
        ShippingMethods = Read.List(subscription.Field("shipping_methods"), method => new ShippingMethod(method));
        ShippingMethod = shippingMethod.ValueKind == JsonValueKind.Object ? new ShippingMethod(shippingMethod) : null;
        Subtotal = Read.String(subscription.Field("subtotal"));
        ShippingAmount = Read.String(subscription.Field("shipping_amount"));
        TaxAmount = Read.String(subscription.Field("tax_amount"));
        Amount = Read.String(subscription.Field("amount"));
        Currency = Read.Enum<Currency>(subscription.Field("currency"));
        IsTest = Read.OptionalBool(subscription.Field("is_test"));
        Renewal = new Renewal(subscription.Field("renewal"));
        NextPaymentAt = Read.NonEmptyString(subscription.Field("next_payment_at"));
        CancelledAt = Read.NonEmptyString(subscription.Field("cancelled_at"));
        CreatedAt = Read.NonEmptyString(subscription.Field("created_at"));
        CheckoutUrl = Read.NonEmptyString(subscription.Field("checkout_url"));
        Customer = customer.ValueKind == JsonValueKind.Object ? new NamedCustomer(customer) : null;
    }

    /// <summary>The subscription's token in the gateway; name it to ask after or change it later.</summary>
    public string Token { get; }

    /// <summary>The channel the subscription was opened on.</summary>
    public string ChannelToken { get; }

    /// <summary>The key the subscription is known by in the calling system.</summary>
    public string ChannelReference { get; }

    public string? Description { get; }

    /// <summary>The account the renewals are taken through; null when the team's Gate rules and default account decided.</summary>
    public string? PaymentProviderToken { get; }

    /// <summary>Where it stands.</summary>
    public SubscriptionStatus Status { get; }

    /// <summary>How often a renewal comes round.</summary>
    public Period Period { get; }

    /// <summary>How many renewals are paid in all; null for one that runs until it is called off.</summary>
    public int? RenewalLimit { get; }

    /// <summary>How many renewals have been paid so far.</summary>
    public int RenewalsPaid { get; }

    /// <summary>What is subscribed to.</summary>
    public IReadOnlyList<Item> Items { get; }

    /// <summary>The ways the goods may be sent, as the merchant offered them.</summary>
    public IReadOnlyList<ShippingMethod> ShippingMethods { get; }

    /// <summary>The way the payer picked; null until they have, or when none was offered.</summary>
    public ShippingMethod? ShippingMethod { get; }

    /// <summary>What the lines come to before tax.</summary>
    public string Subtotal { get; }

    /// <summary>What the picked way of sending comes to before tax.</summary>
    public string ShippingAmount { get; }

    /// <summary>The tax the lines and the sending carry.</summary>
    public string TaxAmount { get; }

    /// <summary>What a renewal comes to now, added up by the gateway.</summary>
    public string Amount { get; }

    public Currency Currency { get; }

    /// <summary>Whether it was opened in the test environment.</summary>
    public bool? IsTest { get; }

    /// <summary>The renewal it is on: what it is charged, the stretch it covers and whether it has been paid.</summary>
    public Renewal Renewal { get; }

    /// <summary>When the next renewal is taken from the kept card; null when none will be.</summary>
    public string? NextPaymentAt { get; }

    /// <summary>When it was called off; null while it has not been.</summary>
    public string? CancelledAt { get; }

    public string? CreatedAt { get; }

    /// <summary>Where the customer pays the renewal it is on, while that is still owed; null otherwise.</summary>
    public string? CheckoutUrl { get; }

    /// <summary>Who is subscribed; null while nobody has said.</summary>
    public NamedCustomer? Customer { get; }

    /// <summary>
    /// Whether the subscription is being paid for: a customer who has been
    /// through the checkout and whose card has not since been turned away.
    /// </summary>
    public bool IsActive => Status == SubscriptionStatus.Active;

    /// <summary>
    /// Whether the first renewal has yet to be paid for. A subscription stays
    /// here until the customer has been through the checkout.
    /// </summary>
    public bool IsPending => Status == SubscriptionStatus.Pending;

    /// <summary>
    /// Whether a renewal has been left unpaid: the card was tried and turned
    /// away every time, and the customer has to pay it themselves at
    /// <see cref="CheckoutUrl"/>.
    /// </summary>
    public bool IsPastDue => Status == SubscriptionStatus.PastDue;

    /// <summary>
    /// Whether it was called off. A subscription called off while a paid
    /// renewal is still running is served to its end.
    /// </summary>
    public bool IsCancelled => Status == SubscriptionStatus.Cancelled;

    /// <summary>Whether every renewal of a limited subscription has been paid.</summary>
    public bool IsCompleted => Status == SubscriptionStatus.Completed;
}

/// <summary>
/// One renewal of a subscription: what it is charged and the stretch of time
/// it pays for.
/// </summary>
public sealed record Renewal
{
    internal Renewal(JsonElement renewal)
    {
        Token = Read.String(renewal.Field("token"));
        Amount = Read.String(renewal.Field("amount"));
        Currency = Read.Enum<Currency>(renewal.Field("currency"));
        StartsAt = Read.NonEmptyString(renewal.Field("starts_at"));
        EndsAt = Read.NonEmptyString(renewal.Field("ends_at"));
        PaidAt = Read.NonEmptyString(renewal.Field("paid_at"));
    }

    public string Token { get; }

    /// <summary>What the renewal is charged, with the kurus behind a point.</summary>
    public string Amount { get; }

    public Currency Currency { get; }

    /// <summary>When the stretch it pays for begins; null until it is paid.</summary>
    public string? StartsAt { get; }

    /// <summary>When the stretch it pays for runs out; null until it is paid.</summary>
    public string? EndsAt { get; }

    /// <summary>When it was paid; null while it is owed.</summary>
    public string? PaidAt { get; }

    /// <summary>Whether it has been paid.</summary>
    public bool IsPaid => PaidAt is not null;
}

/// <summary>
/// A subscription opened, changed, called off or asked after. Whose it is is
/// said beside the subscription, as the answer says it, and on the
/// subscription as well.
/// </summary>
public sealed record SubscriptionDetails
{
    internal SubscriptionDetails(JsonElement body)
    {
        Result = new Result(body);
        Subscription = new Subscription(body);
        Customer = Subscription.Customer;
    }

    public Result Result { get; }

    public Subscription Subscription { get; }

    /// <summary>Who is subscribed; null while nobody has said. The same as <c>Subscription.Customer</c>.</summary>
    public NamedCustomer? Customer { get; }
}

/// <summary>
/// Every subscription opened on a channel within a span of days, oldest first.
/// </summary>
public sealed record SubscriptionList
{
    internal SubscriptionList(JsonElement body)
    {
        Result = new Result(body);
        CreatedFrom = Read.String(body.Field("created_from"));
        CreatedTo = Read.String(body.Field("created_to"));
        Subscriptions = Read.List(body.Field("subscriptions"), subscription => new Subscription(subscription, subscription.Field("customer")));
    }

    public Result Result { get; }

    /// <summary>The first day looked at, as <c>YYYY-MM-DD</c> in the team's timezone.</summary>
    public string CreatedFrom { get; }

    /// <summary>The last day looked at, the same way.</summary>
    public string CreatedTo { get; }

    /// <summary>The subscriptions, oldest first, each with whose it is.</summary>
    public IReadOnlyList<Subscription> Subscriptions { get; }
}
