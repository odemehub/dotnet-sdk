using System.Text.Json.Nodes;
using Odemehub.Enums;

namespace Odemehub.Requests;

/// <summary>
/// A payment handed to the gateway. What is common to every kind of payment
/// lives here; the endpoint it is sent to is what tells the kinds apart.
/// </summary>
/// <remarks>
/// A payment is made with a card the customer typed in or with one they let
/// the merchant keep, never with both. With a kept card the payment goes
/// through the account the card is kept at, so no account is named either;
/// the gateway turns down a payment that names both.
/// </remarks>
public abstract class Payment : Message
{
    /// <summary>
    /// The reference the payment is known by in the calling system, such as
    /// SIP-10231. It has to carry at least one digit: its digits end the order
    /// number the bank is sent, so the payment can be found in the bank's panel.
    /// </summary>
    public required string Reference { get; init; }

    /// <summary>
    /// The amount, as digits with the kurus behind a point: "100", "100.1" or
    /// "100.10". A comma is refused. It is a string so that it is signed and
    /// sent exactly as it is written here, with no rounding on the way.
    /// </summary>
    public required string Amount { get; init; }

    /// <summary>1 to 12. More than one only when the payment is asked for and charged in lira.</summary>
    public required int InstallmentNumber { get; init; }

    /// <summary>The address the customer is paying from, as the merchant sees it.</summary>
    public required string Ip { get; init; }

    /// <summary>
    /// Who is paying: the whole billing address and, when the merchant keeps
    /// them, their reference. A payment that keeps its card, or is made with a
    /// kept one, has to name the customer the card is theirs.
    /// </summary>
    public required Customer Customer { get; init; }

    /// <summary>The card typed in. Left out only when a kept card is named instead.</summary>
    public Card? Card { get; init; }

    /// <summary>A card the customer let the merchant keep, by the token the gateway gave it.</summary>
    public string? SavedCardToken { get; init; }

    /// <summary>Left out, the gateway takes the lira.</summary>
    public Currency? Currency { get; init; }

    /// <summary>
    /// The payment account to charge through. Left out, the team's routing
    /// rules pick the account, and the team's default account is used when
    /// none of them holds. Never named together with a kept card.
    /// </summary>
    public string? PaymentProviderToken { get; init; }

    /// <summary>
    /// What is being sold, where the customer spreads the amount over months
    /// and the bank takes something for the waiting on top of it. Never more
    /// than the amount. Left out where the two are the same, which is most
    /// payments.
    /// </summary>
    public string? BaseAmount { get; init; }

    /// <summary>
    /// The request body. The signature is not part of it; the client signs the
    /// body as a whole and sends the signature in a header of its own.
    /// </summary>
    internal override JsonObject ToBody()
    {
        return Fields.Said(
            ("transaction", Fields.Said(
                ("reference", Reference),
                ("payment_provider_token", PaymentProviderToken),
                ("amount", Amount),
                ("base_amount", BaseAmount),
                ("currency", Wire.Of(Currency)),
                ("installment_number", InstallmentNumber),
                ("ip", Ip),
                ("saved_card_token", SavedCardToken))),
            ("customer", Customer.ToBody()),
            ("card", Card?.ToBody()));
    }
}

/// <summary>
/// A payment the customer confirms with their bank. The gateway does not
/// settle it; it hands back the address the customer has to be sent to, and
/// posts them back to <see cref="CallbackUrl"/> once they are done.
/// </summary>
public sealed class SecurePayment : Payment
{
    /// <summary>
    /// Where the customer's browser is posted back to once they are done at
    /// their bank, with the payment's token, the reference and a hint at how it
    /// went. An address reachable from the internet.
    /// </summary>
    public required string CallbackUrl { get; init; }

    internal override string Path => "secure-payment";

    internal override JsonObject ToBody()
    {
        var body = base.ToBody();
        body["transaction"]!["callback_url"] = CallbackUrl;

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
    /// larger. A payment charged in another money by the team's conversion
    /// rules is refunded in the money it was charged in.
    /// </summary>
    public string? Amount { get; init; }

    internal override string Path => "refund-payment";

    internal override JsonObject ToBody()
    {
        var body = base.ToBody();

        if (Amount is not null)
        {
            body["amount"] = Amount;
        }

        return body;
    }
}

/// <summary>
/// The whole of a payment taken back before the provider settles it. There
/// is no amount to name: a cancellation is always for the whole of it, and a
/// payment part of which has already been refunded can only be refunded for
/// the rest.
/// </summary>
public sealed class CancelPayment : PaymentMessage
{
    internal override string Path => "cancel-payment";
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

    /// <summary>The money the payment is taken in; the lira unless another is named. Instalments are only answered for lira.</summary>
    public Currency? Currency { get; init; }

    internal override string Path => "retrieve-bin";

    internal override JsonObject ToBody()
    {
        return Fields.Of(
            ("transaction", Fields.Said(
                ("payment_provider_token", PaymentProviderToken),
                ("amount", Amount),
                ("currency", Wire.Of(Currency)))),
            ("card", Fields.Of(("bin", Bin))));
    }
}

/// <summary>
/// Payments asked after: one by its token, every attempt made under the
/// merchant's reference, or the ones made between two days — the ones the
/// bank turned away included.
/// </summary>
public sealed class RetrievePayments : Retrieve
{
    internal override string Path => "retrieve-payments";
}
