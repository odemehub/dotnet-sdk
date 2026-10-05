using System.Collections.Generic;
using System.Text.Json.Nodes;
using Odemehub.Enums;

namespace Odemehub.Requests;

/// <summary>
/// A payment link: a page on the gateway that is paid again and again, by
/// anybody who has the address, until it is switched off or its day runs out.
/// There is no customer; whoever pays says who they are on the page. The
/// answer carries the checkout address, which is the link itself.
/// </summary>
/// <remarks>
/// Opening is idempotent per reference: opening again under a
/// reference that already has a link overwrites that link with what is sent
/// and answers with it, under its own token — a link is never used up. Only a
/// link with a payment under way is left alone. A link opened without a
/// reference is given one of the form <c>LINK{n}</c>.
/// </remarks>
public sealed class CreatePaymentLink : Message
{
    /// <summary>What the link is for; at least one line, at most a hundred.</summary>
    public required IReadOnlyList<Item> Items { get; init; }

    public required Currency Currency { get; init; }

    /// <summary>The reference the link is known by in the calling system. Has to carry at least one digit. Left out, the gateway makes one up.</summary>
    public string? Reference { get; init; }

    public string? Description { get; init; }

    /// <summary>The account the link is paid through; it has to take 3D payments. Left out, Gate rules and the default account decide when somebody pays.</summary>
    public string? PaymentProviderToken { get; init; }

    /// <summary>The last day the link may be paid, as <c>YYYY-MM-DD</c> in the team's timezone; today or later. Left out, it never runs out.</summary>
    public string? ExpiresAt { get; init; }

    /// <summary>Whether the link takes payments. Left out, it does.</summary>
    public bool? IsActive { get; init; }

    internal override string Path => "create-payment-link";

    internal override JsonObject ToBody()
    {
        return Fields.Of(
            ("payment_link", Fields.Said(
                ("reference", Reference),
                ("description", Description),
                ("payment_provider_token", PaymentProviderToken),
                ("currency", Wire.Of<Currency>(Currency)),
                ("expires_at", ExpiresAt),
                ("is_active", IsActive),
                ("items", Item.ToBody(Items)))));
    }
}




/// <summary>
/// A change to a payment link, named by its token in the address and again in
/// the body. Only what is sent is written: a field left out keeps what there
/// was, and lines sent replace every line there was. Switching a link off is
/// <c>IsActive = false</c>; switching an expired one back on needs a new
/// <see cref="ExpiresAt"/> with it. A link with a payment under way cannot be
/// changed; the gateway says so on <c>token</c>.
/// </summary>
public sealed class UpdatePaymentLink : Message
{
    /// <summary>The link's token in the gateway.</summary>
    public required string Token { get; init; }

    /// <summary>Sent, they replace every line there was.</summary>
    public IReadOnlyList<Item>? Items { get; init; }

    public Currency? Currency { get; init; }

    public string? Reference { get; init; }

    public string? Description { get; init; }

    public string? PaymentProviderToken { get; init; }

    /// <summary>As <c>YYYY-MM-DD</c> in the team's timezone; today or later.</summary>
    public string? ExpiresAt { get; init; }

    public bool? IsActive { get; init; }

    /// <summary>
    /// Fields to set to nothing, by their names in the body:
    /// <c>expires_at</c> (never runs out), <c>description</c>,
    /// <c>payment_provider_token</c>.
    /// </summary>
    public IReadOnlyList<string>? Clear { get; init; }

    internal override string Path => $"update-payment-link/{Token}";

    internal override JsonObject ToBody()
    {
        var link = Fields.Said(
            ("reference", Reference),
            ("description", Description),
            ("payment_provider_token", PaymentProviderToken),
            ("currency", Wire.Of(Currency)),
            ("expires_at", ExpiresAt),
            ("is_active", IsActive),
            ("items", Item.ToBody(Items)));

        return Fields.Of(
            ("token", Token),
            ("payment_link", CheckoutMessage.Cleared(link, Clear)));
    }
}

/// <summary>
/// Payment links asked after, each with how many payments were made on it and
/// the latest fifty of them.
/// </summary>
public sealed class RetrievePaymentLinks : Retrieve
{
    internal override string Path => "retrieve-payment-links";
}
