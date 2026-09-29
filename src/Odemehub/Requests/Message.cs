using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// Something handed to the gateway. Everything sent there is plain JSON,
/// signed as a whole by the client, so what is common to all of them is the
/// endpoint it goes to and the body it is sent as.
/// </summary>
/// <remarks>
/// The body is built with the client's channel handed in, because a message
/// that speaks for a channel puts it where its own endpoint expects it; one
/// that does not, such as a refund, simply never reads it.
/// </remarks>
public abstract class Message
{
    /// <summary>
    /// The endpoint this is sent to, under the team's gateway.
    /// </summary>
    internal abstract string Path { get; }

    /// <summary>
    /// The request body, in the snake_case the gateway speaks.
    /// </summary>
    internal abstract JsonObject ToBody(string channelToken);
}

/// <summary>
/// A message that speaks for one of the team's channels: a payment, an order
/// opened for checkout, a card kept for a customer. The channel belongs to
/// the integration rather than to any one message, so it is named once on the
/// client; a merchant selling on more than one channel names another here, on
/// the single message that belongs elsewhere.
/// </summary>
public abstract class ChannelMessage : Message
{
    /// <summary>The channel this one message speaks for. Left out, the client's own is used.</summary>
    public string? ChannelToken { get; init; }

    /// <summary>
    /// The channel this message is for: the one it names, or the client's.
    /// </summary>
    internal string Channel(string channelToken)
    {
        return ChannelToken ?? channelToken;
    }
}

/// <summary>
/// Something asked of a payment that has already been made. The payment is
/// named by the token the gateway gave it, and nothing else is sent: the
/// gateway holds the account, the provider, the channel and the reference the
/// provider knows the payment by.
/// </summary>
public abstract class PaymentMessage : Message
{
    /// <summary>The payment's token in the gateway, as it answered when the payment was made.</summary>
    public required string TransactionToken { get; init; }

    internal override JsonObject ToBody(string channelToken)
    {
        return Fields.Of(("transaction", Fields.Of(("token", TransactionToken))));
    }
}

/// <summary>
/// Something asked of a subscription that has already been opened. The
/// subscription is named by the token the gateway gave it, and nothing else is
/// sent: the gateway holds the products, the customer, the channel and the
/// periods it has been through.
/// </summary>
public abstract class SubscriptionMessage : Message
{
    /// <summary>The subscription's token in the gateway, as it answered when it was opened.</summary>
    public required string SubscriptionToken { get; init; }

    internal override JsonObject ToBody(string channelToken)
    {
        return Fields.Of(("subscription", Fields.Of(("token", SubscriptionToken))));
    }
}

/// <summary>
/// Something done to one of a customer's kept cards. The card is named by the
/// token the gateway gave it, and the customer alongside it, so a card can
/// only ever be reached through the customer it belongs to.
/// </summary>
public abstract class SavedCardMessage : ChannelMessage
{
    public required NamedCustomer Customer { get; init; }

    /// <summary>The card's token in the gateway, as a listing of the customer's cards gave it.</summary>
    public required string SavedCardToken { get; init; }

    internal override JsonObject ToBody(string channelToken)
    {
        return Fields.Of(
            ("customer", Customer.ToBody(Channel(channelToken))),
            ("saved_card", Fields.Of(("token", SavedCardToken))));
    }
}
