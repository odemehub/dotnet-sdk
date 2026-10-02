using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Odemehub.Enums;

namespace Odemehub.Responses;

/// <summary>
/// A card a customer let the merchant keep: enough to draw the card and to
/// name it again by its token, and nothing that could charge it.
/// </summary>
public sealed record SavedCard
{
    internal SavedCard(JsonElement card)
    {
        Token = Read.String(card.Field("token"));
        PaymentProviderToken = Read.String(card.Field("payment_provider_token"));
        HolderName = Read.String(card.Field("holder_name"));
        Scheme = Read.OptionalEnum<CardScheme>(card.Field("scheme"));
        FirstDigits = Read.String(card.Field("first_digits"));
        LastFourDigit = Read.String(card.Field("last_four_digit"));
        ExpiryMonth = Read.String(card.Field("expiry_month"));
        ExpiryYear = Read.String(card.Field("expiry_year"));
        IsDefault = Read.Bool(card.Field("is_default"));
        CreatedAt = Read.NonEmptyString(card.Field("created_at"));
    }

    /// <summary>The card's token in the gateway, which names it again later.</summary>
    public string Token { get; }

    /// <summary>The account the card is kept at; it can only be charged there.</summary>
    public string PaymentProviderToken { get; }

    public string HolderName { get; }

    /// <summary>The network the card belongs to, as far as it is known.</summary>
    public CardScheme? Scheme { get; }

    /// <summary>The head of the number: eight digits, or six for a number shorter than sixteen digits.</summary>
    public string FirstDigits { get; }

    public string LastFourDigit { get; }

    public string ExpiryMonth { get; }

    public string ExpiryYear { get; }

    /// <summary>Whether this is the card the customer pays with unless they say otherwise.</summary>
    public bool IsDefault { get; }

    public string? CreatedAt { get; }
}

/// <summary>
/// The customer a card is kept for, by the key the merchant keeps them under:
/// what the card is found by again.
/// </summary>
public sealed record SavedCardCustomer
{
    internal SavedCardCustomer(JsonElement customer)
    {
        Reference = Read.String(customer.Field("reference"));
    }

    public string Reference { get; }
}

/// <summary>
/// A card kept, made the default or asked after.
/// </summary>
public sealed record SavedCardDetails
{
    internal SavedCardDetails(JsonElement body)
    {
        var savedCard = body.Field("saved_card");

        Result = new Result(body);
        SavedCard = savedCard.ValueKind == JsonValueKind.Object ? new SavedCard(savedCard) : null;
        Customer = new SavedCardCustomer(body.Field("customer"));
    }

    public Result Result { get; }

    /// <summary>The card as it now stands; null when keeping it did not work.</summary>
    public SavedCard? SavedCard { get; }

    /// <summary>The customer the card belongs to.</summary>
    public SavedCardCustomer Customer { get; }
}

/// <summary>
/// The cards a customer let the merchant keep.
/// </summary>
public sealed record SavedCardList
{
    internal SavedCardList(JsonElement body)
    {
        Result = new Result(body);
        SavedCards = Read.List(body.Field("saved_cards"), card => new SavedCard(card));
        Customer = new SavedCardCustomer(body.Field("customer"));
    }

    public Result Result { get; }

    public IReadOnlyList<SavedCard> SavedCards { get; }

    /// <summary>The customer the cards belong to.</summary>
    public SavedCardCustomer Customer { get; }

    /// <summary>The card the customer pays with unless they say otherwise; null when they have none.</summary>
    public SavedCard? Default => SavedCards.FirstOrDefault(card => card.IsDefault);
}

/// <summary>
/// A kept card let go of — or not, when the provider would not let it go; the
/// result says which.
/// </summary>
public sealed record DeletedSavedCard
{
    internal DeletedSavedCard(JsonElement body)
    {
        Result = new Result(body);
        SavedCardToken = Read.String(body.Field("saved_card").Field("token"));
        Customer = new SavedCardCustomer(body.Field("customer"));
    }

    public Result Result { get; }

    /// <summary>The token of the card that was asked to be let go of.</summary>
    public string SavedCardToken { get; }

    /// <summary>The customer the card belonged to.</summary>
    public SavedCardCustomer Customer { get; }
}

/// <summary>
/// One way an amount may be paid off on a card.
/// </summary>
public sealed record Installment
{
    internal Installment(JsonElement installment)
    {
        Number = Read.Int(installment.Field("number"));
        Amount = Read.String(installment.Field("amount"));
        Total = Read.String(installment.Field("total"));
    }

    public int Number { get; }

    /// <summary>What is charged each month, as digits with the kurus behind a point.</summary>
    public string Amount { get; }

    /// <summary>What the card is charged in all, the same way.</summary>
    public string Total { get; }
}

/// <summary>
/// What the gateway's provider knows about a card by the head of its number,
/// and how an amount may be paid off on it.
/// </summary>
public sealed record Bin
{
    internal Bin(JsonElement body)
    {
        var card = body.Field("card");

        Result = new Result(body);
        Number = Read.String(card.Field("bin"));
        IssuerName = Read.OptionalString(card.Field("issuer_name"));
        IssuerCode = Read.OptionalString(card.Field("issuer_code"));
        Scheme = Read.OptionalEnum<CardScheme>(card.Field("scheme"));
        Type = Read.OptionalEnum<CardType>(card.Field("type"));
        Program = Read.OptionalString(card.Field("program"));
        IsCommercial = Read.OptionalBool(card.Field("is_commercial"));
        Installments = Read.List(body.Field("installments"), installment => new Installment(installment));
    }

    public Result Result { get; }

    /// <summary>The digits the question was asked with.</summary>
    public string Number { get; }

    /// <summary>The institution that issued the card.</summary>
    public string? IssuerName { get; }

    public string? IssuerCode { get; }

    /// <summary>The scheme the card is issued on, as the issuer reports it.</summary>
    public CardScheme? Scheme { get; }

    /// <summary>Whether the money is lent, drawn from an account or loaded beforehand.</summary>
    public CardType? Type { get; }

    /// <summary>The programme the card is sold under, such as Bonus or Maximum.</summary>
    public string? Program { get; }

    /// <summary>Whether the card belongs to a company rather than to a person; null when it is not known.</summary>
    public bool? IsCommercial { get; }

    /// <summary>The ways the amount may be paid off, a single payment first. Empty for anything but lira.</summary>
    public IReadOnlyList<Installment> Installments { get; }
}
