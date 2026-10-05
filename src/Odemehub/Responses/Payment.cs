using System.Text.Json;
using Odemehub.Enums;

namespace Odemehub.Responses;

/// <summary>
/// How a request went, as every answer opens: whether it worked and, only when
/// it did not, what went wrong. Something that worked has nothing to say
/// beyond that it did.
/// </summary>
public sealed record Result
{
    internal Result(JsonElement body)
    {
        var result = body.Field("result");
        IsSuccessful = Read.Bool(result.Field("successful"));
        Message = Read.NonEmptyString(result.Field("message"));
    }

    public bool IsSuccessful { get; }

    /// <summary>What went wrong, for a request that did not work; null otherwise.</summary>
    public string? Message { get; }
}

/// <summary>
/// What reached the card, for a payment the merchant's conversion rules
/// charged in another money than it was asked in.
/// </summary>
public sealed record Conversion
{
    internal Conversion(JsonElement conversion)
    {
        Amount = Read.String(conversion.Field("amount"));
        Currency = Read.Enum<Currency>(conversion.Field("currency"));
        Rate = Read.String(conversion.Field("rate"));
    }

    /// <summary>What was taken from the card.</summary>
    public string Amount { get; }

    /// <summary>The money it was taken in.</summary>
    public Currency Currency { get; }

    /// <summary>What a unit of the asked-for money was charged as, with any margin on top.</summary>
    public string Rate { get; }
}

/// <summary>
/// The outcome of a payment, as the gateway reports it — whether it answers
/// straight away, is asked after by the payment's token or the merchant's
/// reference, or posts the outcome back once the customer is home from their
/// bank. The shape is the answer's own: how it went (<c>result</c>), the
/// payment (<c>transaction</c>), whose it was (<c>customer</c>), what reached
/// the card in another money (<c>conversion</c>) and the card it kept
/// (<c>saved_card</c>).
/// </summary>
/// <remarks>
/// A payment that was turned down is an outcome like any other and arrives
/// here; only answers that were never a payment outcome are thrown as
/// exceptions.
/// </remarks>
public record Payment
{
    internal Payment(JsonElement body)
    {
        var customer = body.Field("customer");
        var savedCard = body.Field("saved_card");
        var conversion = body.Field("conversion");

        Result = new Result(body);
        Transaction = new PaymentTransaction(body.Field("transaction"));
        Customer = customer.ValueKind == JsonValueKind.Object ? new PaymentCustomer(customer) : null;
        Conversion = conversion.ValueKind == JsonValueKind.Object ? new Conversion(conversion) : null;
        SavedCard = savedCard.ValueKind == JsonValueKind.Object ? new SavedCard(savedCard) : null;
    }

    public Result Result { get; }

    /// <summary>The payment itself.</summary>
    public PaymentTransaction Transaction { get; }

    /// <summary>
    /// Who the payment was made for, as the payment wrote them down: the
    /// merchant's reference for them — null for a payer nobody keeps one for —
    /// and the billing address.
    /// </summary>
    public PaymentCustomer? Customer { get; }

    /// <summary>
    /// What reached the card, for a payment the merchant's conversion rules
    /// charged in another money than it was asked in; null for a payment
    /// charged as it was asked.
    /// </summary>
    public Conversion? Conversion { get; }

    /// <summary>
    /// The card the payment kept, for a payment that asked for one to be kept.
    /// It is null while nothing was kept: because the payment did not go
    /// through, because the provider handed nothing back, or because the
    /// payment never asked.
    /// </summary>
    public SavedCard? SavedCard { get; }
}

/// <summary>
/// A payment as an outcome says it, in full. A payment made at an order, a
/// payment link or a subscription names it, so a webhook about one of them can
/// be checked against the payment it names.
/// </summary>
public sealed record PaymentTransaction
{
    internal PaymentTransaction(JsonElement transaction)
    {
        Token = Read.String(transaction.Field("token"));
        Reference = Read.String(transaction.Field("reference"));
        Status = Read.OptionalEnum<TransactionStatus>(transaction.Field("status"));
        PaymentStatus = Read.OptionalEnum<PaymentStatus>(transaction.Field("payment_status"));
        SecurityType = Read.OptionalEnum<SecurityType>(transaction.Field("security_type"));
        Amount = Read.OptionalString(transaction.Field("amount"));
        BaseAmount = Read.OptionalString(transaction.Field("base_amount"));
        Currency = Read.OptionalEnum<Currency>(transaction.Field("currency"));
        InstallmentNumber = Read.OptionalInt(transaction.Field("installment_number"));
        IsTest = Read.OptionalBool(transaction.Field("is_test"));
        CreatedAt = Read.NonEmptyString(transaction.Field("created_at"));
        OrderToken = Read.NonEmptyString(transaction.Field("order").Field("token"));
        PaymentLinkToken = Read.NonEmptyString(transaction.Field("payment_link").Field("token"));
        SubscriptionToken = Read.NonEmptyString(transaction.Field("subscription").Field("token"));
    }

    /// <summary>The payment's token in the gateway, which names it again to ask after it or give money back.</summary>
    public string Token { get; }


    /// <summary>The reference the payment is known by in the calling system.</summary>
    public string Reference { get; }

    /// <summary>The attempt's state.</summary>
    public TransactionStatus? Status { get; }

    /// <summary>What became of the money.</summary>
    public PaymentStatus? PaymentStatus { get; }

    /// <summary>How it was made: confirmed at the bank or charged straight to the card.</summary>
    public SecurityType? SecurityType { get; }

    /// <summary>What the card was charged, with the kurus behind a point.</summary>
    public string? Amount { get; }

    /// <summary>What was being sold, before anything added for instalments.</summary>
    public string? BaseAmount { get; }

    public Currency? Currency { get; }

    public int? InstallmentNumber { get; }

    /// <summary>Whether it was made in the test environment.</summary>
    public bool? IsTest { get; }

    /// <summary>When the attempt was made, in UTC.</summary>
    public string? CreatedAt { get; }

    /// <summary>The order the payment was made at; null when it was made at none.</summary>
    public string? OrderToken { get; }

    /// <summary>The payment link the payment was made on; null when it was made on none.</summary>
    public string? PaymentLinkToken { get; }

    /// <summary>The subscription whose renewal the payment paid; null when it paid none.</summary>
    public string? SubscriptionToken { get; }

    /// <summary>Whether the attempt went through.</summary>
    public bool IsSuccessful => Status == TransactionStatus.Successful;
}

/// <summary>
/// A 3D payment that has been started. A successful answer is not a settled
/// payment: the customer still has to be sent to <see cref="RedirectUrl"/>.
/// </summary>
public sealed record SecurePayment : Payment
{
    internal SecurePayment(JsonElement body) : base(body)
    {
        RedirectUrl = Read.NonEmptyString(body.Field("result").Field("redirect_url"));
    }

    /// <summary>Where the customer has to be sent. Always there when the payment started.</summary>
    public string? RedirectUrl { get; }
}

/// <summary>
/// A payment charged straight to the card. A successful answer is a settled
/// payment.
/// </summary>
public sealed record RegularPayment : Payment
{
    internal RegularPayment(JsonElement body) : base(body)
    {
    }
}

/// <summary>
/// Money given back out of a payment: a cancellation or a refund. The payment
/// is said as it stands after it.
/// </summary>
public sealed record GiveBack : Payment
{
    internal GiveBack(JsonElement body) : base(body)
    {
        var refund = body.Field("refund");
        Refund = refund.ValueKind == JsonValueKind.Object ? new Refund(refund) : null;
    }

    /// <summary>What was given back.</summary>
    public Refund? Refund { get; }
}

/// <summary>
/// What a cancellation or a refund gave back.
/// </summary>
public sealed record Refund
{
    internal Refund(JsonElement refund)
    {
        Type = Read.Enum<RefundType>(refund.Field("type"));
        Amount = Read.String(refund.Field("amount"));
    }

    /// <summary>Which of the two it was.</summary>
    public RefundType Type { get; }

    /// <summary>How much went back, whether or not it was asked for by name.</summary>
    public string Amount { get; }
}
