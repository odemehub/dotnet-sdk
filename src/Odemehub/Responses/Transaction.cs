using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Odemehub.Enums;

namespace Odemehub.Responses;

/// <summary>
/// One attempt at a payment, as the gateway lists it: enough to tell the
/// attempts apart and see where each got to. Where it stands is said twice on
/// purpose — the attempt's own state, and what became of the money, which can
/// move on to refunded long after the attempt is over. A payment made at an
/// order, a payment link or a subscription names it.
/// </summary>
public sealed record Transaction
{
    internal Transaction(JsonElement transaction)
    {
        var customer = transaction.Field("customer");
        var conversion = transaction.Field("conversion");
        var installmentNumber = Read.Int(transaction.Field("installment_number"));

        Token = Read.String(transaction.Field("token"));
        ChannelToken = Read.String(transaction.Field("channel_token"));
        ChannelReference = Read.String(transaction.Field("channel_reference"));
        Status = Read.Enum<TransactionStatus>(transaction.Field("status"));
        PaymentStatus = Read.Enum<PaymentStatus>(transaction.Field("payment_status"));
        SecurityType = Read.Enum<SecurityType>(transaction.Field("security_type"));
        Amount = Read.String(transaction.Field("amount"));
        BaseAmount = Read.String(transaction.Field("base_amount"));
        Currency = Read.Enum<Currency>(transaction.Field("currency"));
        InstallmentNumber = installmentNumber == 0 ? 1 : installmentNumber;
        IsTest = Read.Bool(transaction.Field("is_test"));
        ErrorCode = Read.NonEmptyString(transaction.Field("error_code"));
        ErrorMessage = Read.NonEmptyString(transaction.Field("error_message"));
        CreatedAt = Read.NonEmptyString(transaction.Field("created_at"));
        Customer = customer.ValueKind == JsonValueKind.Object ? new PaymentCustomer(customer) : null;
        Conversion = conversion.ValueKind == JsonValueKind.Object ? new Conversion(conversion) : null;
        OrderToken = Read.NonEmptyString(transaction.Field("order").Field("token"));
        PaymentLinkToken = Read.NonEmptyString(transaction.Field("payment_link").Field("token"));
        SubscriptionToken = Read.NonEmptyString(transaction.Field("subscription").Field("token"));
    }

    /// <summary>The payment's token in the gateway, which names it again to ask after or give back.</summary>
    public string Token { get; }

    /// <summary>The channel the payment came in on.</summary>
    public string ChannelToken { get; }

    /// <summary>The reference the payment was made under in the calling system.</summary>
    public string ChannelReference { get; }

    /// <summary>The attempt's state.</summary>
    public TransactionStatus Status { get; }

    /// <summary>What became of the money.</summary>
    public PaymentStatus PaymentStatus { get; }

    /// <summary>How it was made: confirmed at the bank or charged straight to the card.</summary>
    public SecurityType SecurityType { get; }

    /// <summary>What the card was charged, with the kurus behind a point.</summary>
    public string Amount { get; }

    /// <summary>What was being sold, before anything added for instalments.</summary>
    public string BaseAmount { get; }

    public Currency Currency { get; }

    public int InstallmentNumber { get; }

    /// <summary>Whether it was made in the test environment.</summary>
    public bool IsTest { get; }

    /// <summary>What the provider called the refusal, for an attempt that failed; null otherwise.</summary>
    public string? ErrorCode { get; }

    /// <summary>Why it failed, written for a person; null otherwise.</summary>
    public string? ErrorMessage { get; }

    /// <summary>When the attempt was made, in UTC.</summary>
    public string? CreatedAt { get; }

    /// <summary>Who the payment was made for, as the payment wrote them down.</summary>
    public PaymentCustomer? Customer { get; }

    /// <summary>What reached the card when it was charged in another money; null when charged as asked.</summary>
    public Conversion? Conversion { get; }

    /// <summary>The token of the order this attempt was at; null when it was at none.</summary>
    public string? OrderToken { get; }

    /// <summary>The token of the payment link this attempt was at; null when it was at none.</summary>
    public string? PaymentLinkToken { get; }

    /// <summary>The token of the subscription this attempt paid a renewal of; null when it paid none.</summary>
    public string? SubscriptionToken { get; }

    /// <summary>Whether the attempt went through.</summary>
    public bool IsSuccessful => Status == TransactionStatus.Successful;
}

/// <summary>
/// Every payment attempt made on a channel within a span of days, oldest
/// first, so they read as the attempts were made.
/// </summary>
public sealed record PaymentList
{
    internal PaymentList(JsonElement body)
    {
        Result = new Result(body);
        CreatedFrom = Read.String(body.Field("created_from"));
        CreatedTo = Read.String(body.Field("created_to"));
        Payments = Read.List(body.Field("payments"), transaction => new Transaction(transaction));
    }

    public Result Result { get; }

    /// <summary>The first day looked at, as <c>YYYY-MM-DD</c> in the team's timezone.</summary>
    public string CreatedFrom { get; }

    /// <summary>The last day looked at, the same way.</summary>
    public string CreatedTo { get; }

    /// <summary>The attempts, oldest first.</summary>
    public IReadOnlyList<Transaction> Payments { get; }

    /// <summary>The attempts that went through.</summary>
    public IReadOnlyList<Transaction> Successful => Payments.Where(payment => payment.IsSuccessful).ToArray();
}
