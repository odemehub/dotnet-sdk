using System.Collections.Generic;
using System.Text.Json.Nodes;
using Odemehub.Enums;

namespace Odemehub.Requests;

/// <summary>
/// An order or a subscription, opened or changed, to be paid on the gateway's
/// own checkout page. Nothing is charged here: the answer carries the address
/// to send the customer to, and they give their card there.
/// </summary>
/// <remarks>
/// What it comes to is not sent. The gateway adds up the lines and the
/// shipping method the payer picks and answers with the amount, so the total
/// can never disagree with what it is made up of. The customer is whatever is
/// known: it is filled in on the checkout page and the payer is asked for the
/// rest.
///
/// Opening is idempotent per channel and reference: opening again under a
/// reference that already has an open order or an unpaid subscription
/// overwrites it with what is sent and answers with the one that was there,
/// under its own token. A paid order, or a subscription that has been paid,
/// is not touched, and neither is one with a payment under way — the gateway
/// says so on <c>channel_reference</c>.
/// </remarks>
public abstract class CheckoutMessage : ChannelMessage
{
    /// <summary>Where the customer goes if they turn back without paying; shown as a link on the checkout page.</summary>
    public string? CancelUrl { get; init; }

    public string? Description { get; init; }

    /// <summary>Left out, the gateway takes the lira.</summary>
    public Currency? Currency { get; init; }

    /// <summary>
    /// The payment account it is paid through. Left out, the merchant's Gate
    /// rules pick the account when the customer pays, and its default account
    /// is used where none of them holds.
    /// </summary>
    public string? PaymentProviderToken { get; init; }

    /// <summary>Whether the checkout page asks the payer where the goods go.</summary>
    public bool? RequiresShippingAddress { get; init; }

    /// <summary>
    /// How the goods may be sent, for the payer to pick from; up to twenty.
    /// Sent on a change, they replace the ones there were, and an empty list
    /// removes them all.
    /// </summary>
    public IReadOnlyList<ShippingMethod>? ShippingMethods { get; init; }

    /// <summary>
    /// The group's fields, with what the caller left unsaid left out.
    /// </summary>
    /// <param name="channel">The channel to write; null to leave it as it is.</param>
    /// <param name="channelReference">The reference, as the kind holds it.</param>
    /// <param name="successUrl">The success address, as the kind holds it.</param>
    /// <param name="items">The lines, as the kind holds them; null for none sent.</param>
    internal JsonObject Details(string? channel, string? channelReference, string? successUrl, IReadOnlyList<Item>? items)
    {
        return Fields.Said(
            ("channel_token", channel),
            ("channel_reference", channelReference),
            ("description", Description),
            ("payment_provider_token", PaymentProviderToken),
            ("currency", Wire.Of(Currency)),
            ("success_url", successUrl),
            ("cancel_url", CancelUrl),
            ("requires_shipping_address", RequiresShippingAddress),
            ("items", Item.ToBody(items)),
            ("shipping_methods", ShippingMethod.ToBody(ShippingMethods)));
    }

    /// <summary>
    /// The body: the token of the one being changed, the group under its own
    /// key and, beside it, the customer when one was given.
    /// </summary>
    internal static JsonObject Body(string group, JsonObject details, Customer? customer, string? token = null)
    {
        return Fields.Said(
            ("token", token),
            (group, details),
            ("customer", customer?.ToBody()));
    }

    /// <summary>
    /// Fields a change sets to nothing. Leaving a field out of a change keeps
    /// what there was, so clearing one — lifting a renewal limit, dropping a
    /// description — has to be said on purpose.
    /// </summary>
    internal static JsonObject Cleared(JsonObject group, IReadOnlyList<string>? clear)
    {
        foreach (var field in clear ?? [])
        {
            group[field] = null;
        }

        return group;
    }
}
