using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// An order opened to be paid once on the gateway's own checkout page. The
/// answer carries the checkout address; the customer is sent there, pays, and
/// is posted back to <see cref="SuccessUrl"/>. The addresses the team set
/// under Webhook in the panel hear that it was paid whether or not the
/// customer comes back. The customer may be left out, or sent without a
/// reference: the payer then says who they are on the checkout, and is not
/// kept as one of the team's customers.
/// </summary>
public sealed class CreateOrder : CheckoutMessage
{
    /// <summary>The number the order is known by in the calling system. Has to carry at least one digit; it need not be unique.</summary>
    public required string Reference { get; init; }

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

    internal override JsonObject ToBody()
    {
        return Body("order", Details(Reference, SuccessUrl, Items), Customer);
    }
}




/// <summary>
/// A change to an open order, named by its token in the address and again in
/// the body. Only what is sent is written: a field left out keeps what there
/// was, lines sent replace every line there was, and the customer sent is
/// written over the one the order had; a reference sent takes the place of the
/// one there was. A paid order, or one with a payment under way, cannot be
/// changed; the gateway says so on <c>token</c>.
/// </summary>
public sealed class UpdateOrder : CheckoutMessage
{
    /// <summary>The order's token in the gateway.</summary>
    public required string Token { get; init; }

    public string? Reference { get; init; }

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

    internal override JsonObject ToBody()
    {
        return Body("order", Cleared(Details(Reference, SuccessUrl, Items), Clear), Customer, Token);
    }
}

/// <summary>Orders asked after, each with its customer.</summary>
public sealed class RetrieveOrders : Retrieve
{
    internal override string Path => "retrieve-orders";
}
