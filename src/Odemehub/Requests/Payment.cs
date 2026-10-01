using System;
using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// A payment handed to the gateway. What is common to every kind of payment
/// lives here; the endpoint it is sent to is what tells the kinds apart.
/// </summary>
/// <remarks>
/// A payment is made with a card the customer typed in or with one they let
/// the merchant keep, never with both: naming a kept card and a card at once
/// is turned down by the gateway, so it is turned down here first.
/// </remarks>
public abstract class Payment : ChannelMessage
{
    /// <summary>
    /// The reference the payment is known by in the calling system, such as
    /// SIP-10231. It has to carry at least one digit: its digits end the order
    /// number the bank is sent, so the payment can be found in the bank's panel.
    /// </summary>
    public required string ChannelReference { get; init; }

    /// <summary>
    /// The amount, as digits with the kurus behind a point: "100", "100.1" or
    /// "100.10". A comma is refused. It is a string so that it is signed and
    /// sent exactly as it is written here, with no rounding on the way.
    /// </summary>
    public required string Amount { get; init; }

    public required int InstallmentNumber { get; init; }

    /// <summary>The address the customer is paying from, as the merchant sees it.</summary>
    public required string Ip { get; init; }

    public required Customer Customer { get; init; }

    /// <summary>The card typed in. Left out only when a kept card is named instead.</summary>
    public Card? Card { get; init; }

    /// <summary>A card the customer let the merchant keep, by the token the gateway gave it.</summary>
    public string? SavedCardToken { get; init; }

    /// <summary>Three letters, e.g. TRY. Left out, the gateway takes the lira.</summary>
    public string? Currency { get; init; }

    /// <summary>
    /// The payment account to charge through. Left out, the team's routing
    /// rules pick the account, and the team's default account is used when
    /// none of them holds. A payment with a kept card always goes through the
    /// account the card is kept at.
    /// </summary>
    public string? PaymentProviderToken { get; init; }

    /// <summary>
    /// What is being sold, where the customer spreads the amount over months
    /// and the bank takes something for the waiting on top of it. Left out
    /// where the two are the same, which is most payments.
    /// </summary>
    public string? BaseAmount { get; init; }

    /// <summary>
    /// The request body. The signature is not part of it; the client signs the
    /// body as a whole and sends the signature in a header of its own.
    /// </summary>
    internal override JsonObject ToBody(string channelToken)
    {
        if ((Card is null) == (SavedCardToken is null))
        {
            throw new ArgumentException("Bir ödeme ya bir kartla ya da kayıtlı bir kartla yapılır; ikisi birden ya da hiçbiri verilemez.");
        }

        var body = Fields.Of(
            ("transaction", Fields.Said(
                ("channel_token", Channel(channelToken)),
                ("channel_reference", ChannelReference),
                ("payment_provider_token", PaymentProviderToken),
                ("amount", Amount),
                ("base_amount", BaseAmount),
                ("currency", Currency),
                ("installment_number", InstallmentNumber),
                ("ip", Ip),
                ("saved_card_token", SavedCardToken))),
            ("customer", Customer.ToBody()));

        if (Card is not null)
        {
            body["card"] = Card.ToBody();
        }

        return body;
    }
}

/// <summary>
/// A payment the customer confirms with their bank. The gateway does not
/// settle it; it hands back the address the customer has to be sent to, and
/// posts them back to <see cref="CallbackUrl"/> once they are done.
/// </summary>
public sealed class SecurePayment : Payment
{
    /// <summary>Where the customer is posted back to, with the signed outcome, once they are done at their bank.</summary>
    public required string CallbackUrl { get; init; }

    /// <summary>
    /// Where the merchant's own server is told how the payment went, signed
    /// the way every answer is. The customer's browser carries the word to
    /// <see cref="CallbackUrl"/> only if the customer stays for it; this
    /// address hears either way, including when the customer never opened the
    /// bank's page and the payment expired.
    /// </summary>
    public string? WebhookUrl { get; init; }

    internal override string Path => "secure-payment";

    internal override JsonObject ToBody(string channelToken)
    {
        var body = base.ToBody(channelToken);
        body["transaction"]!["callback_url"] = CallbackUrl;

        if (WebhookUrl is not null)
        {
            body["transaction"]!["webhook_url"] = WebhookUrl;
        }

        return body;
    }
}

/// <summary>
/// A payment charged straight to the card, without sending the customer to
/// their bank to confirm it. A successful answer is a settled payment.
/// </summary>
public sealed class RegularPayment : Payment
{
    internal override string Path => "regular-payment";
}

/// <summary>
/// Money given back out of a payment the provider has already settled, whole
/// or in part.
/// </summary>
public sealed class RefundPayment : PaymentMessage
{
    /// <summary>
    /// How much goes back, as digits with the kurus behind a point: "35.50".
    /// Leave it out and everything the payment has left in it goes back. It is
    /// never more than the payment has left: the gateway turns down anything
    /// larger.
    /// </summary>
    public string? Amount { get; init; }

    internal override string Path => "refund-payment";

    internal override JsonObject ToBody(string channelToken)
    {
        var body = base.ToBody(channelToken);

        if (Amount is not null)
        {
            body["amount"] = Amount;
        }

        return body;
    }
}

/// <summary>
/// The whole of a payment taken back before the provider has settled it.
/// There is no amount to name: a cancellation is always for the whole of the
/// payment, and anything less goes back as a refund.
/// </summary>
public sealed class CancelPayment : PaymentMessage
{
    internal override string Path => "cancel-payment";
}

/// <summary>
/// How a payment went, asked for after the fact. A customer sent to their bank
/// comes back carrying the payment's token and nothing more, because a browser
/// cannot be given anything to sign with; this is the call that says what
/// became of it.
/// </summary>
public sealed class RetrievePayment : PaymentMessage
{
    internal override string Path => "retrieve-payment";
}

/// <summary>
/// A question about a card before anything is charged to it: who issued it,
/// what kind of card it is, and how the amount may be paid off on it. Only the
/// head of the number is sent, never the whole of it.
/// </summary>
public sealed class RetrieveBin : Message
{
    /// <summary>
    /// The first six to eight digits of the card. Six is what banks key their
    /// tables on; eight is what the gateway keeps of a card it has been paid
    /// with, so a stored card's digits can be sent as they are.
    /// </summary>
    public required string Bin { get; init; }

    /// <summary>What the payment would come to, as digits with the kurus behind a point: "1000.00".</summary>
    public required string Amount { get; init; }

    /// <summary>
    /// The account to ask. Left out, the account the team's routing rules
    /// would send the card to is asked — the default one when none of them
    /// holds — so the instalments match a payment that names no account either.
    /// </summary>
    public string? PaymentProviderToken { get; init; }

    /// <summary>The money the payment is taken in; the lira unless another is named.</summary>
    public string? Currency { get; init; }

    internal override string Path => "retrieve-bin";

    internal override JsonObject ToBody(string channelToken)
    {
        return Fields.Of(
            ("transaction", Fields.Said(
                ("payment_provider_token", PaymentProviderToken),
                ("amount", Amount),
                ("currency", Currency))),
            ("card", Fields.Of(("bin", Bin))));
    }
}
