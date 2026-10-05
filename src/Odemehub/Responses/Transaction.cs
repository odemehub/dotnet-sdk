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
        Reference = Read.String(transaction.Field("reference"));
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
        SavedCard = transaction.Field("saved_card").ValueKind == JsonValueKind.Object ? new SavedCard(transaction.Field("saved_card")) : null;
    }

    /// <summary>The payment's token in the gateway, which names it again to ask after or give back.</summary>
    public string Token { get; }


    /// <summary>The reference the payment was made under in the calling system.</summary>
    public string Reference { get; }

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
    /// <summary>The card the payment kept, when it asked to keep one and went through; null otherwise.</summary>
    public SavedCard? SavedCard { get; }

    public bool IsSuccessful => Status == TransactionStatus.Successful;
}

/// <summary>
/// Payments asked after, each with its state, amount, customer and what became
/// of its money, the ones the bank turned away included. The answer is always a list, oldest first, and an empty one when
/// nothing matched. The days are the ones the gateway used, when the records
/// were asked for by the days they were made on: the ones asked for, or the
/// last seven when none were.
/// </summary>
public sealed record PaymentList
{
    internal PaymentList(JsonElement body)
    {
        Result = new Result(body);
        CreatedFrom = Read.NonEmptyString(body.Field("created_from"));
        CreatedTo = Read.NonEmptyString(body.Field("created_to"));
        Payments = Read.List(body.Field("payments"), entry => new Transaction(entry));
    }

    public Result Result { get; }

    /// <summary>The first day listed, as <c>YYYY-MM-DD</c> in the team's timezone; null when they were asked for by token or reference.</summary>
    public string? CreatedFrom { get; }

    /// <summary>The last day listed, the same way.</summary>
    public string? CreatedTo { get; }

    public IReadOnlyList<Transaction> Payments { get; }

    /// <summary>The payments that went through.</summary>
    public IReadOnlyList<Transaction> Successful => Payments.Where(payment => payment.IsSuccessful).ToArray();
}
