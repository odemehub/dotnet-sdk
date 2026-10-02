using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Odemehub.Enums;

namespace Odemehub.Responses;

/// <summary>
/// A payment link as it stands: what is sold, what it comes to now, whether
/// it takes payments and until when, the environment its payments are taken
/// in, and the address it is paid at while it can be paid.
/// </summary>
public sealed record PaymentLink
{
    internal PaymentLink(JsonElement link)
    {
        Token = Read.String(link.Field("token"));
        ChannelToken = Read.NonEmptyString(link.Field("channel_token"));
        ChannelReference = Read.String(link.Field("channel_reference"));
        Description = Read.NonEmptyString(link.Field("description"));
        PaymentProviderToken = Read.NonEmptyString(link.Field("payment_provider_token"));
        Items = Read.List(link.Field("items"), item => new Item(item));
        Subtotal = Read.String(link.Field("subtotal"));
        TaxAmount = Read.String(link.Field("tax_amount"));
        Amount = Read.String(link.Field("amount"));
        Currency = Read.Enum<Currency>(link.Field("currency"));
        IsActive = Read.Bool(link.Field("is_active"));
        IsTest = Read.Bool(link.Field("is_test"));
        ExpiresAt = Read.NonEmptyString(link.Field("expires_at"));
        CheckoutUrl = Read.NonEmptyString(link.Field("checkout_url"));
        CreatedAt = Read.NonEmptyString(link.Field("created_at"));
    }

    /// <summary>The link's token in the gateway; name it to ask after or change it later.</summary>
    public string Token { get; }

    /// <summary>The channel the link sells on; null for one on the team's own ödemehub channel.</summary>
    public string? ChannelToken { get; }

    /// <summary>The reference the link is known by, the one sent or the <c>LINK{n}</c> the gateway made up.</summary>
    public string ChannelReference { get; }

    public string? Description { get; }

    /// <summary>The account the link is paid through; null when the team's Gate rules and default account decide.</summary>
    public string? PaymentProviderToken { get; }

    /// <summary>What the link is for.</summary>
    public IReadOnlyList<Item> Items { get; }

    /// <summary>What the lines come to before tax.</summary>
    public string Subtotal { get; }

    /// <summary>The tax the lines carry.</summary>
    public string TaxAmount { get; }

    /// <summary>What one payment on the link comes to, added up by the gateway.</summary>
    public string Amount { get; }

    public Currency Currency { get; }

    /// <summary>Whether it takes payments now: switched on and its last day not gone by.</summary>
    public bool IsActive { get; }

    /// <summary>Whether its payments are taken in the test environment now.</summary>
    public bool IsTest { get; }

    /// <summary>The last moment it may be paid, in UTC; null for one that never runs out.</summary>
    public string? ExpiresAt { get; }

    /// <summary>The link itself, the address it is paid at; null while it cannot be paid.</summary>
    public string? CheckoutUrl { get; }

    public string? CreatedAt { get; }
}

/// <summary>
/// A payment link opened, changed or asked after. Asked after by its token,
/// it also carries how many payments were made on it and the latest fifty of
/// them, newest first.
/// </summary>
public sealed record PaymentLinkDetails
{
    internal PaymentLinkDetails(JsonElement body)
    {
        var link = body.Field("payment_link");

        Result = new Result(body);
        PaymentLink = new PaymentLink(link);
        TransactionsCount = Read.OptionalInt(link.Field("transactions_count"));
        Transactions = Read.List(link.Field("transactions"), transaction => new Transaction(transaction));
    }

    public Result Result { get; }

    public PaymentLink PaymentLink { get; }

    /// <summary>How many payments were made on the link in all; null except when it is asked after by its token.</summary>
    public int? TransactionsCount { get; }

    /// <summary>The latest fifty payments made on the link, newest first; empty except when it is asked after by its token.</summary>
    public IReadOnlyList<Transaction> Transactions { get; }

    /// <summary>The listed payments that went through.</summary>
    public IReadOnlyList<Transaction> Successful => Transactions.Where(transaction => transaction.IsSuccessful).ToArray();
}

/// <summary>
/// Every payment link opened on a channel within a span of days, oldest first.
/// </summary>
public sealed record PaymentLinkList
{
    internal PaymentLinkList(JsonElement body)
    {
        Result = new Result(body);
        CreatedFrom = Read.String(body.Field("created_from"));
        CreatedTo = Read.String(body.Field("created_to"));
        PaymentLinks = Read.List(body.Field("payment_links"), link => new PaymentLink(link));
    }

    public Result Result { get; }

    /// <summary>The first day looked at, as <c>YYYY-MM-DD</c> in the team's timezone.</summary>
    public string CreatedFrom { get; }

    /// <summary>The last day looked at, the same way.</summary>
    public string CreatedTo { get; }

    /// <summary>The links, oldest first.</summary>
    public IReadOnlyList<PaymentLink> PaymentLinks { get; }
}
