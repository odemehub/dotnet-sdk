using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// A card kept for a customer without a payment being made on it. The
/// provider is told who the card belongs to, so the token it hands back is
/// held under that customer and the card can be charged again later. Once the
/// provider takes it, the team's customer under the reference is written from
/// what was sent and the card is kept for them; a payment with the card has to
/// name the same reference.
/// </summary>
/// <remarks>
/// Providers without a card store of their own keep a card by charging a small
/// amount and giving it straight back; those need the security code, and the
/// ones with a real card store do not. It is never stored.
/// </remarks>
public sealed class CreateSavedCard : Message
{
    /// <summary>Who the card belongs to: the reference and the whole billing address, both required.</summary>
    public required Customer Customer { get; init; }

    public required Card Card { get; init; }

    /// <summary>The payment account to keep the card at; it has to keep cards. Left out, the team's default account is used.</summary>
    public string? PaymentProviderToken { get; init; }

    internal override string Path => "create-saved-card";

    internal override JsonObject ToBody()
    {
        var card = Card.ToBody();
        card.Remove("should_save");

        return Fields.Said(
            ("saved_card", PaymentProviderToken is null ? null : Fields.Of(("payment_provider_token", PaymentProviderToken))),
            ("customer", Customer.ToBody()),
            ("card", card));
    }
}



/// <summary>
/// A change to a kept card, named by its token in the address and again in
/// the body. The one thing that may be changed is whether it is the
/// customer's default: a card is made the default here, and stops being one
/// when another card of the customer's is made the default instead.
/// </summary>
public sealed class UpdateSavedCard : Message
{
    /// <summary>The card's token in the gateway.</summary>
    public required string Token { get; init; }

    /// <summary>Has to be true; the gateway turns down anything else.</summary>
    public bool IsDefault { get; init; } = true;

    internal override string Path => $"update-saved-card/{Token}";

    internal override JsonObject ToBody()
    {
        return Fields.Of(
            ("token", Token),
            ("saved_card", Fields.Of(("is_default", IsDefault))));
    }
}

/// <summary>
/// Letting go of a kept card, named by its token in the address and again in
/// the body. It is dropped at the provider first and with the gateway after: a
/// card at an account whose provider cannot let a card go stays, and the
/// gateway says so.
/// </summary>
public sealed class DeleteSavedCard : Message
{
    /// <summary>The card's token in the gateway.</summary>
    public required string Token { get; init; }

    internal override string Path => $"delete-saved-card/{Token}";

    internal override JsonObject ToBody()
    {
        return Fields.Of(("token", Token));
    }
}

/// <summary>
/// Kept cards asked after: one by its token, every card of a customer by the
/// merchant's reference for them, or the ones kept between two days. A
/// customer's cards come with the one they pay with by default first.
/// </summary>
public sealed class RetrieveSavedCards : Retrieve
{
    internal override string Path => "retrieve-saved-cards";

    /// <summary>A card is named by the reference of the customer it is kept for.</summary>
    internal override string ReferenceField => "customer_reference";
}
