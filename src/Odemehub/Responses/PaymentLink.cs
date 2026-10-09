using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Odemehub.Enums;

namespace Odemehub.Responses;

/// <summary>
/// A payment link as it stands: what is sold — its lines and what they come to
/// now, or what the payer may pick and the tax on it — in which money,
/// whether it takes payments and until when, the environment its payments
/// are taken in, and the address it is paid at while it can be paid.
/// </summary>
public sealed record PaymentLink
{
    internal PaymentLink(JsonElement link)
    {
        Token = Read.String(link.Field("token"));
        Reference = Read.String(link.Field("reference"));
        Description = Read.NonEmptyString(link.Field("description"));
        PaymentProviderToken = Read.NonEmptyString(link.Field("payment_provider_token"));
        AmountType = Read.Enum<AmountType>(link.Field("amount_type"));
        ItemName = Read.NonEmptyString(link.Field("item_name"));
        PredefinedAmounts = Read.OptionalList(link.Field("predefined_amounts"), Read.String);
        TaxRate = Read.OptionalString(link.Field("tax_rate"));
        TaxMode = Read.Enum<TaxMode>(link.Field("tax_mode"));
        Items = Read.List(link.Field("items"), item => new Item(item));
        Subtotal = Read.OptionalString(link.Field("subtotal"));
        TaxAmount = Read.OptionalString(link.Field("tax_amount"));
        Amount = Read.OptionalString(link.Field("amount"));
        Currency = Read.Enum<Currency>(link.Field("currency"));
        CurrencyType = Read.Enum<CurrencyType>(link.Field("currency_type"));
        Currencies = Read.OptionalList(link.Field("currencies"), Read.Enum<Currency>);
        EmailsCustomer = Read.Bool(link.Field("emails_customer"));
        IsActive = Read.Bool(link.Field("is_active"));
        IsTest = Read.Bool(link.Field("is_test"));
        ExpiresAt = Read.NonEmptyString(link.Field("expires_at"));
        CheckoutUrl = Read.NonEmptyString(link.Field("checkout_url"));
        CreatedAt = Read.NonEmptyString(link.Field("created_at"));
        TransactionsCount = Read.OptionalInt(link.Field("transactions_count"));
        Transactions = Read.List(link.Field("transactions"), transaction => new Transaction(transaction));
    }

    /// <summary>The link's token in the gateway; name it to ask after or change it later.</summary>
    public string Token { get; }


    /// <summary>The reference the link is known by, the one sent or the <c>LINK{n}</c> the gateway made up.</summary>
    public string Reference { get; }

    public string? Description { get; }

    /// <summary>The account the link is paid through; null when the team's Gate rules and default account decide.</summary>
    public string? PaymentProviderToken { get; }

    /// <summary>What the payer pays: the lines, or an amount they pick.</summary>
    public AmountType AmountType { get; }

    /// <summary>The name of the one line an amount the payer picks is paid as; null for a link paid as its lines.</summary>
    public string? ItemName { get; }

    /// <summary>The amounts the payer picks from; null for a link that offers none.</summary>
    public IReadOnlyList<string>? PredefinedAmounts { get; }

    /// <summary>The tax on an amount the payer picks, as a percentage; null for a link paid as its lines, or one without tax.</summary>
    public string? TaxRate { get; }

    /// <summary>Whether <see cref="TaxRate"/> is inside the amount the payer picks or added on top of it.</summary>
    public TaxMode TaxMode { get; }

    /// <summary>What the link is for; empty when the payer picks the amount.</summary>
    public IReadOnlyList<Item> Items { get; }

    /// <summary>What the lines come to before tax; null when the payer picks the amount.</summary>
    public string? Subtotal { get; }

    /// <summary>The tax the lines carry; null when the payer picks the amount.</summary>
    public string? TaxAmount { get; }

    /// <summary>What one payment on the link comes to, added up by the gateway; null when the payer picks the amount.</summary>
    public string? Amount { get; }

    /// <summary>The link's money; the one the payer starts with where they may pick.</summary>
    public Currency Currency { get; }

    /// <summary>Whether the link is paid in its one money or the payer picks one.</summary>
    public CurrencyType CurrencyType { get; }

    /// <summary>The moneys the payer may pick, <see cref="Currency"/> among them; null for a link paid in its one money.</summary>
    public IReadOnlyList<Currency>? Currencies { get; }

    /// <summary>Whether the payer is sent an e-mail, at the address they give on the checkout page, once their payment goes through.</summary>
    public bool EmailsCustomer { get; }

    /// <summary>Whether it takes payments now: switched on and its last day not gone by.</summary>
    public bool IsActive { get; }

    /// <summary>Whether its payments are taken in the test environment now.</summary>
    public bool IsTest { get; }

    /// <summary>The last moment it may be paid, in UTC; null for one that never runs out.</summary>
    public string? ExpiresAt { get; }

    /// <summary>The link itself, the address it is paid at; null while it cannot be paid.</summary>
    public string? CheckoutUrl { get; }

    public string? CreatedAt { get; }


    /// <summary>How many payments were made on the link in all, however many are listed; null but on a listed link.</summary>
    public int? TransactionsCount { get; }

    /// <summary>The latest fifty payments made on the link, newest first, the refused ones included; listed links only.</summary>
    public IReadOnlyList<Transaction> Transactions { get; }

    /// <summary>The listed payments that went through.</summary>
    public IReadOnlyList<Transaction> Successful => Transactions.Where(transaction => transaction.IsSuccessful).ToArray();
}

/// <summary>
/// The answer to opening or changing a payment link: the link as it now
/// stands. Its latest payment attempts are on the link when it is asked after
/// with <c>RetrievePaymentLinksAsync</c>, and the payments made at it, one
/// per payer, with <c>RetrieveLinkPaymentsAsync</c>.
/// </summary>
public sealed record PaymentLinkDetails
{
    internal PaymentLinkDetails(JsonElement body)
    {
        Result = new Result(body);
        PaymentLink = new PaymentLink(body.Field("payment_link"));
    }

    public Result Result { get; }

    public PaymentLink PaymentLink { get; }
}

/// <summary>
/// Payment links asked after, each with how many payments were made on it and
/// the latest fifty of them. The answer is always a list, oldest first, and an empty one when
/// nothing matched. The days are the ones the gateway used, when the records
/// were asked for by the days they were made on: the ones asked for, or the
/// last seven when none were.
/// </summary>
public sealed record PaymentLinkList
{
    internal PaymentLinkList(JsonElement body)
    {
        Result = new Result(body);
        CreatedFrom = Read.NonEmptyString(body.Field("created_from"));
        CreatedTo = Read.NonEmptyString(body.Field("created_to"));
        PaymentLinks = Read.List(body.Field("payment_links"), entry => new PaymentLink(entry));
    }

    public Result Result { get; }

    /// <summary>The first day listed, as <c>YYYY-MM-DD</c> in the team's timezone; null when they were asked for by token or reference.</summary>
    public string? CreatedFrom { get; }

    /// <summary>The last day listed, the same way.</summary>
    public string? CreatedTo { get; }

    public IReadOnlyList<PaymentLink> PaymentLinks { get; }
}
