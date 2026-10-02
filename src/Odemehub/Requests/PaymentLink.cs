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
/// Opening is idempotent per channel and reference: opening again under a
/// reference that already has a link overwrites that link with what is sent
/// and answers with it, under its own token — a link is never used up. Only a
/// link with a payment under way is left alone. A link opened without a
/// reference is given one of the form <c>LINK{n}</c>.
///
/// Give <see cref="ChannelMessage.OdemehubChannel"/> as the channel to open
/// the link on the team's own ödemehub channel, where the panel opens its
/// links.
/// </remarks>
public sealed class CreatePaymentLink : ChannelMessage
{
    /// <summary>What the link is for; at least one line, at most a hundred.</summary>
    public required IReadOnlyList<Item> Items { get; init; }

    public required Currency Currency { get; init; }

    /// <summary>The reference the link is known by in the calling system. Has to carry at least one digit. Left out, the gateway makes one up.</summary>
    public string? ChannelReference { get; init; }

    public string? Description { get; init; }

    /// <summary>The account the link is paid through; it has to take 3D payments. Left out, Gate rules and the default account decide when somebody pays.</summary>
    public string? PaymentProviderToken { get; init; }

    /// <summary>The last day the link may be paid, as <c>YYYY-MM-DD</c> in the team's timezone; today or later. Left out, it never runs out.</summary>
    public string? ExpiresAt { get; init; }

    /// <summary>Whether the link takes payments. Left out, it does.</summary>
    public bool? IsActive { get; init; }

    internal override string Path => "create-payment-link";

    internal override JsonObject ToBody(string channelToken)
    {
        return Fields.Of(
            ("payment_link", Fields.Said(
                ("channel_token", LinkChannel(channelToken)),
                ("channel_reference", ChannelReference),
                ("description", Description),
                ("payment_provider_token", PaymentProviderToken),
                ("currency", Wire.Of<Currency>(Currency)),
                ("expires_at", ExpiresAt),
                ("is_active", IsActive),
                ("items", Item.ToBody(Items)))));
    }
}

/// <summary>
/// A payment link as it stands, by its token, with how many payments were
/// made on it and the latest fifty of them, newest first, the ones the bank
/// turned away included. The rest are read with
/// <see cref="RetrievePaymentsByChannelReference"/>.
/// </summary>
public sealed class RetrievePaymentLink : RetrieveByToken
{
    internal override string Endpoint => "retrieve-payment-link";
}

/// <summary>
/// A payment link as it stands, by the merchant's own reference for it on a
/// channel. Give <see cref="ChannelMessage.OdemehubChannel"/> as the channel to
/// find a link on the team's own ödemehub channel, which is where the panel
/// opens its links.
/// </summary>
public sealed class RetrievePaymentLinkByReference : RetrieveByReference
{
    internal override string Path => "retrieve-payment-link-by-reference";

    internal override JsonObject ToBody(string channelToken)
    {
        return Fields.Said(
            ("channel_token", LinkChannel(channelToken)),
            ("channel_reference", ChannelReference));
    }
}

/// <summary>
/// Every payment link opened on a channel within a span of days, oldest
/// first. Give <see cref="ChannelMessage.OdemehubChannel"/> as the channel to
/// list the links on the team's own ödemehub channel.
/// </summary>
public sealed class RetrievePaymentLinksByChannelReference : RetrieveByChannelReference
{
    internal override string Path => "retrieve-payment-links-by-channel-reference";

    internal override JsonObject ToBody(string channelToken)
    {
        return Fields.Said(
            ("channel_token", LinkChannel(channelToken)),
            ("created_from", CreatedFrom),
            ("created_to", CreatedTo));
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
/// <remarks>
/// The channel is written only when this message names one; the client's own
/// is not sent. <see cref="ChannelMessage.OdemehubChannel"/> moves the link to
/// the team's own ödemehub channel.
/// </remarks>
public sealed class UpdatePaymentLink : ChannelMessage
{
    /// <summary>The link's token in the gateway.</summary>
    public required string Token { get; init; }

    /// <summary>Sent, they replace every line there was.</summary>
    public IReadOnlyList<Item>? Items { get; init; }

    public Currency? Currency { get; init; }

    public string? ChannelReference { get; init; }

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

    internal override JsonObject ToBody(string channelToken)
    {
        var link = Fields.Said(
            ("channel_reference", ChannelReference),
            ("description", Description),
            ("payment_provider_token", PaymentProviderToken),
            ("currency", Wire.Of(Currency)),
            ("expires_at", ExpiresAt),
            ("is_active", IsActive),
            ("items", Item.ToBody(Items)));

        if (ChannelToken is not null)
        {
            link["channel_token"] = LinkChannel(channelToken);
        }

        return Fields.Of(
            ("token", Token),
            ("payment_link", CheckoutMessage.Cleared(link, Clear)));
    }
}
