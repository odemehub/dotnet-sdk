namespace Odemehub.Enums;

/// <summary>
/// What a webhook says happened, as the gateway writes it in
/// <see cref="Responses.Webhook.Event"/>. The first part is what it is about —
/// <c>order</c>, <c>payment_link</c>, <c>subscription</c>,
/// <c>transaction</c> — and the webhook carries that thing's token.
/// </summary>
/// <remarks>
/// Kept as strings rather than an enum: the names carry a dot, which the
/// client's enums are not written with on the wire, and an event this version
/// does not know still compares as the text it is.
/// </remarks>
public static class WebhookEvent
{
    public const string OrderPaid = "order.paid";
    public const string OrderPaymentRefunded = "order.payment_refunded";
    public const string OrderPaymentCancelled = "order.payment_cancelled";
    public const string PaymentLinkPaid = "payment_link.paid";
    public const string PaymentLinkPaymentRefunded = "payment_link.payment_refunded";
    public const string PaymentLinkPaymentCancelled = "payment_link.payment_cancelled";
    public const string SubscriptionActive = "subscription.active";
    public const string SubscriptionPastDue = "subscription.past_due";
    public const string SubscriptionCancelled = "subscription.cancelled";
    public const string SubscriptionEnded = "subscription.ended";
    public const string SubscriptionCompleted = "subscription.completed";
    public const string SubscriptionPaymentRefunded = "subscription.payment_refunded";
    public const string SubscriptionPaymentCancelled = "subscription.payment_cancelled";
    public const string TransactionSuccessful = "transaction.successful";
    public const string TransactionFailed = "transaction.failed";
    public const string TransactionExpired = "transaction.expired";
    public const string TransactionPaymentRefunded = "transaction.payment_refunded";
    public const string TransactionPaymentCancelled = "transaction.payment_cancelled";
}
