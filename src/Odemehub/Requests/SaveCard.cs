using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// A card kept for a customer without a payment being made on it. The
/// provider is told who the card belongs to, so the token it hands back is
/// held under that customer and the card can be charged again later.
/// </summary>
/// <remarks>
/// Providers without a card store of their own keep a card by charging one
/// lira and giving it straight back; those ask for the security code, and the
/// ones with a real card store do not.
/// </remarks>
public sealed class SaveCard : ChannelMessage
{
    public required Customer Customer { get; init; }

    public required Card Card { get; init; }

    /// <summary>The payment account to keep the card at. Left out, the team's default account is used.</summary>
    public string? PaymentProviderToken { get; init; }

    internal override string Path => "save-card";

    internal override JsonObject ToBody(string channelToken)
    {
        var card = Card.ToBody();
        card.Remove("should_save");

        if (Card.SecurityCode == "")
        {
            card.Remove("security_code");
        }

        return Fields.Of(
            ("saved_card", Fields.Said(
                ("channel_token", Channel(channelToken)),
                ("payment_provider_token", PaymentProviderToken))),
            ("customer", Customer.ToBody()),
            ("card", card));
    }
}

/// <summary>
/// The cards a customer has let the merchant keep, asked for by naming the
/// customer. The card that is theirs by default comes first.
/// </summary>
public sealed class SavedCards : ChannelMessage
{
    public required NamedCustomer Customer { get; init; }

    internal override string Path => "saved-cards";

    internal override JsonObject ToBody(string channelToken)
    {
        return Fields.Of(("customer", Customer.ToBody(Channel(channelToken))));
    }
}

/// <summary>
/// Making one of a customer's kept cards the one they pay with unless they say
/// otherwise. A customer has one such card; the one that was it before stops
/// being it as this one is written down.
/// </summary>
public sealed class DefaultSavedCard : SavedCardMessage
{
    internal override string Path => "default-saved-card";
}

/// <summary>
/// Letting go of a kept card. It is dropped at the provider first and with the
/// gateway after: a card the provider would not let go of stays, and the
/// answer says why.
/// </summary>
public sealed class DeleteSavedCard : SavedCardMessage
{
    internal override string Path => "delete-saved-card";
}
