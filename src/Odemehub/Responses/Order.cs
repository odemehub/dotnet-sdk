using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Odemehub.Responses;

/// <summary>
/// An order as the gateway keeps it: what is being paid for, what it comes
/// to, where it stands and — once it is paid — the payment that paid it. The
/// same answer comes back whether the order has just been opened, asked
/// after, or the gateway is telling the merchant it was paid.
/// </summary>
/// <remarks>
/// Nothing is charged when an order is opened: the customer has to be sent to
/// <see cref="CheckoutUrl"/> and gives their card there. What becomes of it is
/// posted to the merchant's webhook address, if it gave one, and is always
/// there to be asked after by the order's token.
/// </remarks>
public sealed record Order
{
    internal Order(JsonElement body)
    {
        var order = body.Field("order");

        Result = new Result(body);
        Token = Read.String(order.Field("token"));
        ChannelToken = Read.String(order.Field("channel_token"));
        ChannelReference = Read.String(order.Field("channel_reference"));
        Description = Read.NonEmptyString(order.Field("description"));
        Status = Read.String(order.Field("status"));
        Items = Read.List(order.Field("items"), item => new OrderItem(item));
        Subtotal = Read.NonEmptyString(order.Field("subtotal"));
        TaxAmount = Read.NonEmptyString(order.Field("tax_amount"));
        Amount = Read.String(order.Field("amount"));
        Currency = Read.String(order.Field("currency"));
        IsTest = Read.OptionalBool(order.Field("is_test"));
        CreatedAt = Read.NonEmptyString(order.Field("created_at"));
        CheckoutUrl = Read.NonEmptyString(order.Field("checkout_url"));
        TransactionToken = Read.NonEmptyString(order.Field("transaction").Field("token"));
        CustomerChannelReference = Read.NonEmptyString(body.Field("customer").Field("channel_reference"));
    }

    public Result Result { get; }

    /// <summary>The order's token in the gateway; name it to ask after it later.</summary>
    public string Token { get; }

    /// <summary>The channel the order was opened on.</summary>
    public string ChannelToken { get; }

    /// <summary>The number the order is known by in the calling system.</summary>
    public string ChannelReference { get; }

    public string? Description { get; }

    /// <summary>Where the order stands: open until it is paid, then paid.</summary>
    public string Status { get; }

    /// <summary>What the order is made up of.</summary>
    public IReadOnlyList<OrderItem> Items { get; }

    /// <summary>What the lines come to before tax; null when no line carried a rate.</summary>
    public string? Subtotal { get; }

    /// <summary>The tax the order carries; null when no line carried a rate.</summary>
    public string? TaxAmount { get; }

    /// <summary>What the order comes to, added up from its lines by the gateway.</summary>
    public string Amount { get; }

    public string Currency { get; }

    /// <summary>Whether it was paid in the test environment; null until it is paid.</summary>
    public bool? IsTest { get; }

    public string? CreatedAt { get; }

    /// <summary>Where the customer pays, while the order is still open; null once it is paid.</summary>
    public string? CheckoutUrl { get; }

    /// <summary>The token of the payment that paid the order, which names it again for a refund; null while it is open.</summary>
    public string? TransactionToken { get; }

    /// <summary>The merchant's own key for the customer the order is for; null for an order opened without one.</summary>
    public string? CustomerChannelReference { get; }

    /// <summary>Whether the order has been paid.</summary>
    public bool IsPaid => Status == "paid";
}

/// <summary>
/// One line of what an order is made up of, as it was written down when the
/// order was opened: filled in from the catalogue where the line said
/// nothing, and as the line said where it did.
/// </summary>
public sealed record OrderItem
{
    internal OrderItem(JsonElement item)
    {
        ChannelReference = Read.String(item.Field("channel_reference"));
        Name = Read.String(item.Field("name"));
        Image = Read.OptionalString(item.Field("image"));
        Quantity = Read.Int(item.Field("quantity"));
        UnitAmount = Read.String(item.Field("unit_amount"));
        TaxRate = Read.OptionalString(item.Field("tax_rate"));
        TaxAmount = Read.OptionalString(item.Field("tax_amount"));
    }

    /// <summary>The merchant's own key for what is on the line.</summary>
    public string ChannelReference { get; }

    public string Name { get; }

    /// <summary>The picture the line is shown with; null when it has none.</summary>
    public string? Image { get; }

    public int Quantity { get; }

    /// <summary>The price of one, as digits with the kurus behind a point.</summary>
    public string UnitAmount { get; }

    /// <summary>The tax included in the price, as a percentage; null for a line with no rate.</summary>
    public string? TaxRate { get; }

    /// <summary>The tax the line comes to; null for a line with no rate.</summary>
    public string? TaxAmount { get; }
}

/// <summary>
/// Word the gateway sent about one of the merchant's orders: that it was
/// paid, with the payment that paid it. An order is only ever told of once;
/// an attempt that fails leaves it open and the customer trying again.
/// </summary>
public sealed record OrderWebhook
{
    internal OrderWebhook(JsonElement body)
    {
        Event = Read.String(body.Field("event"));
        Order = new Order(body);
    }

    /// <summary>The state reached: paid.</summary>
    public string Event { get; }

    /// <summary>The order as it stands now, with the payment that paid it.</summary>
    public Order Order { get; }

    /// <summary>Whether the order has been paid, which is the one thing said here.</summary>
    public bool IsPaid => Event == "paid";
}

/// <summary>
/// Word the gateway sent about a payment the merchant started and the
/// customer finished — or did not — at their bank. It is the same answer
/// <c>RetrievePaymentAsync</c> gives, with the state reached on top: the
/// customer may have closed the page before their browser could bring the
/// outcome back, and then this is the only word the merchant hears.
/// </summary>
public sealed record TransactionWebhook : Payment
{
    internal TransactionWebhook(JsonElement body) : base(body)
    {
        Event = Read.String(body.Field("event"));
    }

    /// <summary>The state reached: successful, failed or expired.</summary>
    public string Event { get; }

    /// <summary>Whether the payment went through.</summary>
    public bool IsSuccessful => Event == "successful";

    /// <summary>Whether the bank turned the payment away.</summary>
    public bool IsFailed => Event == "failed";

    /// <summary>Whether the customer never opened the bank's page in time, so the payment was closed without being tried.</summary>
    public bool IsExpired => Event == "expired";
}

/// <summary>
/// One attempt at a payment, as the gateway lists it: enough to tell the
/// attempts apart and see where each got to. Where it stands is said twice on
/// purpose — the attempt's own state, and what became of the money, which can
/// move on to refunded long after the attempt is over.
/// </summary>
public sealed record Transaction
{
    internal Transaction(JsonElement transaction)
    {
        var conversion = transaction.Field("conversion");
        var installmentNumber = Read.Int(transaction.Field("installment_number"));

        Token = Read.String(transaction.Field("token"));
        ChannelToken = Read.String(transaction.Field("channel_token"));
        ChannelReference = Read.String(transaction.Field("channel_reference"));
        Status = Read.String(transaction.Field("status"));
        PaymentStatus = Read.String(transaction.Field("payment_status"));
        SecurityType = Read.String(transaction.Field("security_type"));
        Amount = Read.String(transaction.Field("amount"));
        BaseAmount = Read.String(transaction.Field("base_amount"));
        Currency = Read.String(transaction.Field("currency"));
        InstallmentNumber = installmentNumber == 0 ? 1 : installmentNumber;
        IsTest = Read.Bool(transaction.Field("is_test"));
        ErrorCode = Read.NonEmptyString(transaction.Field("error_code"));
        ErrorMessage = Read.NonEmptyString(transaction.Field("error_message"));
        CreatedAt = Read.NonEmptyString(transaction.Field("created_at"));
        CustomerChannelReference = Read.NonEmptyString(transaction.Field("customer").Field("channel_reference"));
        Conversion = conversion.ValueKind == JsonValueKind.Object ? new Conversion(conversion) : null;
        OrderToken = Read.NonEmptyString(transaction.Field("order").Field("token"));
        SubscriptionToken = Read.NonEmptyString(transaction.Field("subscription").Field("token"));
    }

    /// <summary>The payment's token in the gateway, which names it again to ask after or give back.</summary>
    public string Token { get; }

    /// <summary>The channel the payment came in on.</summary>
    public string ChannelToken { get; }

    /// <summary>The reference the payment was made under in the calling system.</summary>
    public string ChannelReference { get; }

    /// <summary>The attempt's state: started, redirected_to_secure_page, returned_from_secure_page, failed, expired or successful.</summary>
    public string Status { get; }

    /// <summary>What became of the money: unpaid, paid, cancelled, refunded or partially_refunded.</summary>
    public string PaymentStatus { get; }

    /// <summary>How it was made: secure (confirmed at the bank) or regular.</summary>
    public string SecurityType { get; }

    /// <summary>What the card was charged, with the kurus behind a point.</summary>
    public string Amount { get; }

    /// <summary>What was being sold, before anything added for instalments.</summary>
    public string BaseAmount { get; }

    public string Currency { get; }

    public int InstallmentNumber { get; }

    /// <summary>Whether it was made in the test environment.</summary>
    public bool IsTest { get; }

    /// <summary>What the provider called the refusal, for an attempt that failed; null otherwise.</summary>
    public string? ErrorCode { get; }

    /// <summary>Why it failed, written for a person; null otherwise.</summary>
    public string? ErrorMessage { get; }

    public string? CreatedAt { get; }

    /// <summary>The merchant's own key for the customer; null for a payer the merchant never named.</summary>
    public string? CustomerChannelReference { get; }

    /// <summary>What reached the card when it was charged in another money; null when charged as asked.</summary>
    public Conversion? Conversion { get; }

    /// <summary>The token of the order this attempt was at; null when it was at none.</summary>
    public string? OrderToken { get; }

    /// <summary>The token of the subscription this attempt paid a period of; null when it paid none.</summary>
    public string? SubscriptionToken { get; }

    /// <summary>Whether the attempt went through.</summary>
    public bool IsSuccessful => Status == "successful";
}

/// <summary>
/// Every attempt made under one of the merchant's own numbers on a channel,
/// oldest first, so they read as the attempts were made.
/// </summary>
public sealed record Transactions
{
    internal Transactions(JsonElement body)
    {
        Result = new Result(body);
        Items = Read.List(body.Field("transactions"), transaction => new Transaction(transaction));
    }

    public Result Result { get; }

    /// <summary>The attempts, oldest first.</summary>
    public IReadOnlyList<Transaction> Items { get; }

    /// <summary>The attempt that went through; null when none did.</summary>
    public Transaction? Successful => Items.FirstOrDefault(transaction => transaction.IsSuccessful);
}
