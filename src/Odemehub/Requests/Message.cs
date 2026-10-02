using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// Something handed to the gateway. Everything sent there is plain JSON,
/// signed as a whole by the client, so what is common to all of them is the
/// endpoint it goes to, the method it goes with and the body it is sent as.
/// </summary>
/// <remarks>
/// The body is built with the client's channel handed in, because a message
/// that speaks for a channel puts it where its own endpoint expects it; one
/// that does not, such as a refund, simply never reads it.
/// </remarks>
public abstract class Message
{
    /// <summary>
    /// The endpoint this is sent to, under the team's gateway, with the token
    /// in the address where the endpoint takes one.
    /// </summary>
    internal abstract string Path { get; }

    /// <summary>
    /// The HTTP method this goes with. Everything is posted, except asking
    /// after one record by its token.
    /// </summary>
    internal virtual string Method => "POST";

    /// <summary>
    /// The request body, in the snake_case the gateway speaks. Never read for
    /// a message sent with GET, which carries nothing but its address.
    /// </summary>
    internal abstract JsonObject ToBody(string channelToken);
}

/// <summary>
/// A message that speaks for one of the team's channels: a payment, an order
/// opened for the checkout, a card kept for a customer. The channel belongs
/// to the integration rather than to any one message, so it is named once on
/// the client; a merchant selling on more than one channel names another
/// here, on the single message that belongs elsewhere.
/// </summary>
public abstract class ChannelMessage : Message
{
    /// <summary>
    /// Stands for the team's own ödemehub channel, which has no token of its
    /// own and is only ever reached by payment links: the panel opens its
    /// links there, and a link that names no channel of the merchant's goes
    /// there too. Give it as the <see cref="ChannelToken"/> of a payment link
    /// message to reach those links.
    /// </summary>
    public const string OdemehubChannel = "odemehub";

    /// <summary>The channel this one message speaks for. Left out, the client's own is used.</summary>
    public string? ChannelToken { get; init; }

    /// <summary>
    /// The channel this message is for: the one it names, or the client's.
    /// </summary>
    internal string Channel(string channelToken)
    {
        return ChannelToken ?? channelToken;
    }

    /// <summary>
    /// The channel a payment link message is for: the one it names, the
    /// client's, or — for <see cref="OdemehubChannel"/> — none, which the
    /// gateway reads as its own ödemehub channel.
    /// </summary>
    internal string? LinkChannel(string channelToken)
    {
        return ChannelToken == OdemehubChannel ? null : Channel(channelToken);
    }
}

/// <summary>
/// Money given back out of a payment that has already been made. The payment
/// is named by the token the gateway gave it, and nothing else is sent: the
/// gateway holds the account, the provider, the channel and the reference the
/// provider knows the payment by.
/// </summary>
public abstract class PaymentMessage : Message
{
    /// <summary>The payment's token in the gateway, as it answered when the payment was made.</summary>
    public required string Token { get; init; }

    internal override JsonObject ToBody(string channelToken)
    {
        return Fields.Of(("transaction", Fields.Of(("token", Token))));
    }
}

/// <summary>
/// One record asked after by the token the gateway gave it, as
/// <c>GET retrieve-{resource}/{token}</c>. There is no body: the signature is
/// taken over the empty string, and the token travels in the address. A
/// caller only ever reaches its own team's records; anybody else's is turned
/// down as though it did not exist (404).
/// </summary>
public abstract class RetrieveByToken : Message
{
    /// <summary>The record's token in the gateway, as it was answered when the record was made.</summary>
    public required string Token { get; init; }

    /// <summary>The endpoint, without the token.</summary>
    internal abstract string Endpoint { get; }

    internal override string Path => $"{Endpoint}/{Token}";

    internal override string Method => "GET";

    internal override JsonObject ToBody(string channelToken)
    {
        return new JsonObject();
    }
}

/// <summary>
/// One record asked after by the merchant's own reference for it on a
/// channel. Where more than one carries the same reference, the one opened
/// last is answered; a reference that names nothing is answered as not found
/// (404). Nothing is changed by asking.
/// </summary>
public abstract class RetrieveByReference : ChannelMessage
{
    /// <summary>The reference the record was made under in the calling system.</summary>
    public required string ChannelReference { get; init; }

    internal override JsonObject ToBody(string channelToken)
    {
        return Fields.Of(
            ("channel_token", Channel(channelToken)),
            ("channel_reference", ChannelReference));
    }
}

/// <summary>
/// Every record of a kind made on a channel within a span of days, oldest
/// first. The days are given as <c>YYYY-MM-DD</c> in the team's own timezone,
/// both ends included, and the span may be at most seven days; the two are
/// given together or not at all, and left out they mean the last seven days
/// up to today. Nothing is changed by asking.
/// </summary>
public abstract class RetrieveByChannelReference : ChannelMessage
{
    /// <summary>The first day, as <c>YYYY-MM-DD</c>. Given together with <see cref="CreatedTo"/>.</summary>
    public string? CreatedFrom { get; init; }

    /// <summary>The last day, as <c>YYYY-MM-DD</c>, at most six days after the first.</summary>
    public string? CreatedTo { get; init; }

    internal override JsonObject ToBody(string channelToken)
    {
        return Fields.Said(
            ("channel_token", Channel(channelToken)),
            ("created_from", CreatedFrom),
            ("created_to", CreatedTo));
    }
}
