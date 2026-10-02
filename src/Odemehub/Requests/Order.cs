using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// An order opened to be paid once on the gateway's own checkout page. The
/// answer carries the checkout address; the customer is sent there, pays, and
/// is posted back to <see cref="SuccessUrl"/>. The addresses set for the
/// order's channel under Webhook in the panel hear that it was paid whether or
/// not the customer comes back.
/// </summary>
public sealed class CreateOrder : CheckoutMessage
{
    /// <summary>The number the order is known by in the calling system. Has to carry at least one digit.</summary>
    public required string ChannelReference { get; init; }

    /// <summary>
    /// Where the customer's browser is posted back to once it is paid, with
    /// the payment's token. An address reachable from the internet.
    /// </summary>
    public required string SuccessUrl { get; init; }

    /// <summary>What the order is for; at least one line, at most a hundred.</summary>
    public required IReadOnlyList<Item> Items { get; init; }

    /// <summary>Who it is for, as far as it is known; any part of it, or none.</summary>
    public Customer? Customer { get; init; }

    internal override string Path => "create-order";

    internal override JsonObject ToBody(string channelToken)
    {
        return Body("order", Details(Channel(channelToken), ChannelReference, SuccessUrl, Items), Customer);
    }
}

/// <summary>
/// Where an order stands, by the token the gateway gave it when it was
/// opened: what it is for, whether it has been paid and, if so, by which
/// payment. This is how a merchant learns what became of an order whose
/// customer never came back from the checkout.
/// </summary>
public sealed class RetrieveOrder : RetrieveByToken
{
    internal override string Endpoint => "retrieve-order";
}

/// <summary>
/// Where the latest order under one of the merchant's own numbers on a
/// channel stands.
/// </summary>
public sealed class RetrieveOrderByReference : RetrieveByReference
{
    internal override string Path => "retrieve-order-by-reference";
}

/// <summary>
/// Every order opened on a channel within a span of days, oldest first, each
/// with whose it is.
/// </summary>
public sealed class RetrieveOrdersByChannelReference : RetrieveByChannelReference
{
    internal override string Path => "retrieve-orders-by-channel-reference";
}

/// <summary>
/// A change to an open order, named by its token in the address and again in
/// the body. Only what is sent is written: a field left out keeps what there
/// was, lines sent replace every line there was, and the customer sent is
/// written over the one the order had. A paid order, or one with a payment
/// under way, cannot be changed; the gateway says so on <c>token</c>.
/// </summary>
/// <remarks>
/// The channel is written only when this message names one; the client's own
/// is not sent, so a change never moves an order between channels by accident.
/// </remarks>
public sealed class UpdateOrder : CheckoutMessage
{
    /// <summary>The order's token in the gateway.</summary>
    public required string Token { get; init; }

    public string? ChannelReference { get; init; }

    public string? SuccessUrl { get; init; }

    /// <summary>Sent, they replace every line there was.</summary>
    public IReadOnlyList<Item>? Items { get; init; }

    public Customer? Customer { get; init; }

    /// <summary>
    /// Fields to set to nothing, by their names in the body:
    /// <c>description</c>, <c>cancel_url</c>, <c>payment_provider_token</c>.
    /// </summary>
    public IReadOnlyList<string>? Clear { get; init; }

    internal override string Path => $"update-order/{Token}";

    internal override JsonObject ToBody(string channelToken)
    {
        return Body("order", Cleared(Details(ChannelToken, ChannelReference, SuccessUrl, Items), Clear), Customer, Token);
    }
}
