using System.Text.Json;

namespace Odemehub.Responses;

/// <summary>
/// A word the gateway sent about something of the merchant's: an order paid,
/// a link paid, a subscription's state changed, a payment finished, money
/// given back. It goes to the addresses the team set for the event under
/// Webhook in the panel, as plain JSON signed the way every answer is.
/// </summary>
/// <remarks>
/// It is a notification, never the answer. It names the thing by token — the
/// payer's payment at it too for a link, and the payment beside it when money
/// moved — and nothing else; ask the gateway what became of it
/// (<c>RetrieveOrdersAsync</c>, <c>RetrieveLinkPaymentsAsync</c>,
/// <c>RetrieveSubscriptionsAsync</c>, <c>RetrievePaymentsAsync</c>) and act on
/// that. A word may arrive more than once; the id tells the copies apart.
/// </remarks>
public sealed record Webhook
{
    internal Webhook(JsonElement body)
    {
        Id = Read.String(body.Field("id"));
        Event = Read.String(body.Field("event"));
        CreatedAt = Read.NonEmptyString(body.Field("created_at"));
        OrderToken = Read.NonEmptyString(body.Field("order").Field("token"));
        PaymentLinkToken = Read.NonEmptyString(body.Field("payment_link").Field("token"));
        LinkPaymentToken = Read.NonEmptyString(body.Field("link_payment").Field("token"));
        SubscriptionToken = Read.NonEmptyString(body.Field("subscription").Field("token"));
        TransactionToken = Read.NonEmptyString(body.Field("transaction").Field("token"));
    }

    /// <summary>The word's own token, the same on every delivery of it.</summary>
    public string Id { get; }

    /// <summary>What happened, as the gateway writes it; see <see cref="Enums.WebhookEvent"/>.</summary>
    public string Event { get; }

    /// <summary>When the word was written, in UTC.</summary>
    public string? CreatedAt { get; }

    /// <summary>The order, for the <c>order.*</c> events.</summary>
    public string? OrderToken { get; }

    /// <summary>The payment link, for the <c>payment_link.*</c> events.</summary>
    public string? PaymentLinkToken { get; }

    /// <summary>The payer's payment at the link, beside the link on the <c>payment_link.*</c> events.</summary>
    public string? LinkPaymentToken { get; }

    /// <summary>The subscription, for the <c>subscription.*</c> events.</summary>
    public string? SubscriptionToken { get; }

    /// <summary>The payment: for the <c>transaction.*</c> events, and beside the thing wherever money moved at it.</summary>
    public string? TransactionToken { get; }
}
