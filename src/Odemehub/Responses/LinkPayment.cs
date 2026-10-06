using System.Collections.Generic;
using System.Text.Json;
using Odemehub.Enums;

namespace Odemehub.Responses;

/// <summary>
/// A payment at a payment link, as the gateway keeps it: one payer paying the
/// link once. What was paid and the tax in it, where it stands, the link it
/// was made at, the payer as they billed themselves and — once it is paid —
/// the payment that paid it, which names it again for a refund.
/// </summary>
/// <remarks>
/// It is opened by the payer as they pay, never by the merchant, and stays
/// open while their bank turns them away; every attempt is a payment of its
/// own under the same payment at the link.
/// </remarks>
public sealed record LinkPayment
{
    internal LinkPayment(JsonElement linkPayment)
    {
        var customer = linkPayment.Field("customer");
        var transaction = linkPayment.Field("transaction");

        Token = Read.String(linkPayment.Field("token"));
        Reference = Read.String(linkPayment.Field("reference"));
        PaymentLink = new PaymentLinkReference(linkPayment.Field("payment_link"));
        PaymentProviderToken = Read.NonEmptyString(linkPayment.Field("payment_provider_token"));
        Status = Read.Enum<LinkPaymentStatus>(linkPayment.Field("status"));
        Items = Read.List(linkPayment.Field("items"), item => new Item(item));
        Subtotal = Read.String(linkPayment.Field("subtotal"));
        TaxAmount = Read.String(linkPayment.Field("tax_amount"));
        Amount = Read.String(linkPayment.Field("amount"));
        Discount = Discount.Of(linkPayment.Field("discount"));
        Currency = Read.Enum<Currency>(linkPayment.Field("currency"));
        Customer = customer.ValueKind == JsonValueKind.Object ? new PaymentCustomer(customer) : null;
        IsTest = Read.Bool(linkPayment.Field("is_test"));
        CreatedAt = Read.NonEmptyString(linkPayment.Field("created_at"));
        Transaction = transaction.ValueKind == JsonValueKind.Object ? new TransactionReference(transaction) : null;
    }

    /// <summary>The payment's token in the gateway; name it to ask after it later.</summary>
    public string Token { get; }

    /// <summary>The reference the gateway gave it, of the form <c>LINKPAY{n}</c>; the payments made for it carry the same one.</summary>
    public string Reference { get; }

    /// <summary>The link it was made at.</summary>
    public PaymentLinkReference PaymentLink { get; }

    /// <summary>The account it is paid through; null when none is set.</summary>
    public string? PaymentProviderToken { get; }

    /// <summary>Where it stands: open until a payment goes through, then paid.</summary>
    public LinkPaymentStatus Status { get; }

    /// <summary>What was paid for: the link's lines, or the one line of the amount the payer picked.</summary>
    public IReadOnlyList<Item> Items { get; }

    /// <summary>What the lines come to before tax, less the coupon.</summary>
    public string Subtotal { get; }

    /// <summary>The tax the lines carry, less the coupon.</summary>
    public string TaxAmount { get; }

    /// <summary>What it comes to, less the coupon.</summary>
    public string Amount { get; }

    /// <summary>The coupon the payer put on it at checkout; null when none was.</summary>
    public Discount? Discount { get; }

    /// <summary>The money it is paid in, the one the payer picked where the link let them.</summary>
    public Currency Currency { get; }

    /// <summary>The payer as they billed themselves on the page, its <c>Reference</c> always null; null until they have.</summary>
    public PaymentCustomer? Customer { get; }

    /// <summary>Whether it was made in the test environment.</summary>
    public bool IsTest { get; }

    /// <summary>When the payer opened it, in UTC.</summary>
    public string? CreatedAt { get; }

    /// <summary>The payment that paid it, which names it again for a refund; null while it is open.</summary>
    public TransactionReference? Transaction { get; }

    /// <summary>Whether it has been paid.</summary>
    public bool IsPaid => Status == LinkPaymentStatus.Paid;
}

/// <summary>
/// The payment link a payment was made at: enough to ask after it.
/// </summary>
public sealed record PaymentLinkReference
{
    internal PaymentLinkReference(JsonElement link)
    {
        Token = Read.String(link.Field("token"));
        Reference = Read.String(link.Field("reference"));
    }

    /// <summary>The link's token in the gateway.</summary>
    public string Token { get; }

    /// <summary>The reference the link is known by.</summary>
    public string Reference { get; }
}

/// <summary>
/// Payments made at the team's links asked after. The answer is always a
/// list, oldest first, and an empty one when nothing matched. The days are
/// the ones the gateway used, when the records were asked for by the days
/// they were made on: the ones asked for, or the last seven when none were.
/// </summary>
public sealed record LinkPaymentList
{
    internal LinkPaymentList(JsonElement body)
    {
        Result = new Result(body);
        CreatedFrom = Read.NonEmptyString(body.Field("created_from"));
        CreatedTo = Read.NonEmptyString(body.Field("created_to"));
        LinkPayments = Read.List(body.Field("link_payments"), entry => new LinkPayment(entry));
    }

    public Result Result { get; }

    /// <summary>The first day listed, as <c>YYYY-MM-DD</c> in the team's timezone; null when they were asked for by token or reference.</summary>
    public string? CreatedFrom { get; }

    /// <summary>The last day listed, the same way.</summary>
    public string? CreatedTo { get; }

    public IReadOnlyList<LinkPayment> LinkPayments { get; }
}
