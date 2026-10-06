using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Odemehub.Enums;

namespace Odemehub.Requests;

/// <summary>
/// A payment link, opened or changed: a page on the gateway that is paid
/// again and again, by anybody who has the address, until it is switched off
/// or its day runs out. There is no customer; whoever pays says who they are
/// on the page. The answer carries the checkout address, which is the link
/// itself.
/// </summary>
/// <remarks>
/// A link is paid as the lines the merchant wrote, or — with an
/// <see cref="AmountType"/> other than <see cref="Enums.AmountType.Fixed"/> —
/// as an amount the payer picks, paid as one line named
/// <see cref="ItemName"/> with the link's own <see cref="TaxRate"/>; the
/// lines are then passed over. It is paid in one money, or — with
/// <see cref="Enums.CurrencyType.Selectable"/> — in the one the payer picks
/// of <see cref="Currencies"/>. Left out, the link is paid as its lines in
/// its one money.
/// </remarks>
public abstract class PaymentLinkMessage : Message
{
    public string? Description { get; init; }

    /// <summary>The account the link is paid through; it has to take 3D payments. Left out, Gate rules and the default account decide when somebody pays.</summary>
    public string? PaymentProviderToken { get; init; }

    /// <summary>What the payer pays: the lines, or an amount they pick. Left out on a new link, it is paid as its lines.</summary>
    public AmountType? AmountType { get; init; }

    /// <summary>The name of the one line an amount the payer picks is paid as; needed whenever the payer picks the amount.</summary>
    public string? ItemName { get; init; }

    /// <summary>The amounts the payer picks from, as digits with the kurus behind a point: "100.00"; one to ten of them, needed when amounts are offered.</summary>
    public IReadOnlyList<string>? PredefinedAmounts { get; init; }

    /// <summary>The tax on an amount the payer picks, as a percentage: "20" or "20.00". Left out, it carries no tax.</summary>
    public string? TaxRate { get; init; }

    /// <summary>Whether <see cref="TaxRate"/> is inside the amount the payer picks or added on top of it. Left out, it is inside.</summary>
    public TaxMode? TaxMode { get; init; }

    /// <summary>Whether the link is paid in its one money or the payer picks one. Left out on a new link, it is paid in its one money.</summary>
    public CurrencyType? CurrencyType { get; init; }

    /// <summary>The other moneys the payer may pick besides the link's own; needed when the payer picks the money.</summary>
    public IReadOnlyList<Currency>? Currencies { get; init; }

    /// <summary>Whether the payer is sent an e-mail once their payment goes through. Left out on a new link, they are not.</summary>
    public bool? EmailsPayer { get; init; }

    /// <summary>The last day the link may be paid, as <c>YYYY-MM-DD</c> in the team's timezone; today or later. Left out on a new link, it never runs out.</summary>
    public string? ExpiresAt { get; init; }

    /// <summary>Whether the link takes payments. Left out on a new link, it does.</summary>
    public bool? IsActive { get; init; }

    /// <summary>
    /// The link's fields, with what the caller left unsaid left out.
    /// </summary>
    /// <param name="reference">The reference, as the kind holds it.</param>
    /// <param name="currency">The money, as the kind holds it.</param>
    /// <param name="items">The lines, as the kind holds them; null for none sent.</param>
    internal JsonObject Details(string? reference, Currency? currency, IReadOnlyList<Item>? items)
    {
        return Fields.Said(
            ("reference", reference),
            ("description", Description),
            ("payment_provider_token", PaymentProviderToken),
            ("amount_type", Wire.Of(AmountType)),
            ("item_name", ItemName),
            ("predefined_amounts", Fields.List(PredefinedAmounts)),
            ("tax_rate", TaxRate),
            ("tax_mode", Wire.Of(TaxMode)),
            ("currency", Wire.Of(currency)),
            ("currency_type", Wire.Of(CurrencyType)),
            ("currencies", Fields.List(Currencies?.Select(money => Wire.Of<Currency>(money)))),
            ("emails_payer", EmailsPayer),
            ("expires_at", ExpiresAt),
            ("is_active", IsActive),
            ("items", Item.ToBody(items)));
    }
}

/// <summary>
/// A payment link opened through the gateway.
/// </summary>
/// <remarks>
/// Every call opens a new link under a new token, even under a reference
/// already sent: the reference is the merchant's own label and need not be
/// unique, so keep the token that comes back. A link opened without a
/// reference is given one of the form <c>LINK{n}</c>.
/// </remarks>
public sealed class CreatePaymentLink : PaymentLinkMessage
{
    public required Currency Currency { get; init; }

    /// <summary>What the link is for, at most a hundred lines; at least one when the link is paid as its lines, passed over when the payer picks the amount.</summary>
    public IReadOnlyList<Item>? Items { get; init; }

    /// <summary>The reference the link is known by in the calling system. Has to carry at least one digit. Left out, the gateway makes one up.</summary>
    public string? Reference { get; init; }

    internal override string Path => "create-payment-link";

    internal override JsonObject ToBody()
    {
        return Fields.Of(("payment_link", Details(Reference, Currency, Items)));
    }
}




/// <summary>
/// A change to a payment link, named by its token in the address and again in
/// the body. Only what is sent is written: a field left out keeps what there
/// was, and lines sent replace every line there was. A link turned back to
/// being paid as its lines has to have lines. Switching a link off is
/// <c>IsActive = false</c>; one that ran out takes payments again once it is
/// given a new <see cref="PaymentLinkMessage.ExpiresAt"/>, or none. A payment
/// under way does not stand in the way: the payments to come are charged as
/// the link now is.
/// </summary>
public sealed class UpdatePaymentLink : PaymentLinkMessage
{
    /// <summary>The link's token in the gateway.</summary>
    public required string Token { get; init; }

    /// <summary>Sent, they replace every line there was.</summary>
    public IReadOnlyList<Item>? Items { get; init; }

    public Currency? Currency { get; init; }

    public string? Reference { get; init; }

    /// <summary>
    /// Fields to set to nothing, by their names in the body:
    /// <c>expires_at</c> (never runs out), <c>description</c>,
    /// <c>payment_provider_token</c>, <c>item_name</c>,
    /// <c>predefined_amounts</c>, <c>tax_rate</c>, <c>currencies</c>.
    /// </summary>
    public IReadOnlyList<string>? Clear { get; init; }

    internal override string Path => $"update-payment-link/{Token}";

    internal override JsonObject ToBody()
    {
        return Fields.Of(
            ("token", Token),
            ("payment_link", CheckoutMessage.Cleared(Details(Reference, Currency, Items), Clear)));
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

/// <summary>
/// The payments made at the team's links asked after: one by its token, by
/// the reference the gateway gave it (<c>LINKPAY{n}</c>), or the ones made
/// between two days. A payment at a link is opened by the payer as they pay,
/// never by the merchant, so it is only ever asked after.
/// </summary>
public sealed class RetrieveLinkPayments : Retrieve
{
    internal override string Path => "retrieve-link-payments";
}
