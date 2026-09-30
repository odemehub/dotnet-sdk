using System.Text.Json;

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
        Currency = Read.String(conversion.Field("currency"));
        Rate = Read.String(conversion.Field("rate"));
    }

    /// <summary>What was taken from the card.</summary>
    public string Amount { get; }

    /// <summary>The money it was taken in, e.g. TRY.</summary>
    public string Currency { get; }

    /// <summary>What a unit of the asked-for money was charged as, with any margin on top.</summary>
    public string Rate { get; }
}

/// <summary>
/// The outcome of a payment, as the gateway reports it — whether it answers
/// straight away or posts the outcome back once the customer is home from
/// their bank. The two are the same shape, so a merchant reads them the same
/// way: how it went, which payment it was, and whose.
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
        var transaction = body.Field("transaction");
        var savedCard = body.Field("saved_card");
        var conversion = body.Field("conversion");

        Result = new Result(body);
        TransactionToken = Read.String(transaction.Field("token"));
        ChannelToken = Read.String(transaction.Field("channel_token"));
        ChannelReference = Read.String(transaction.Field("channel_reference"));
        CustomerChannelReference = Read.String(body.Field("customer").Field("channel_reference"));
        SavedCard = savedCard.ValueKind == JsonValueKind.Object ? new SavedCard(savedCard) : null;
        Conversion = conversion.ValueKind == JsonValueKind.Object ? new Conversion(conversion) : null;
    }

    public Result Result { get; }

    /// <summary>The payment's token in the gateway, which names it again for a refund.</summary>
    public string TransactionToken { get; }

    /// <summary>The channel the payment came in on.</summary>
    public string ChannelToken { get; }

    /// <summary>The reference the payment is known by in the calling system.</summary>
    public string ChannelReference { get; }

    /// <summary>The merchant's own key for the customer the payment was made for.</summary>
    public string CustomerChannelReference { get; }

    /// <summary>
    /// The card the payment kept, for a payment that asked for one to be kept.
    /// It is null while nothing was kept: because the payment did not go
    /// through, because the provider handed nothing back, or because the
    /// payment never asked.
    /// </summary>
    public SavedCard? SavedCard { get; }

    /// <summary>
    /// What reached the card, for a payment the merchant's conversion rules
    /// charged in another money than it was asked in; null for a payment
    /// charged as it was asked.
    /// </summary>
    public Conversion? Conversion { get; }
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
/// Money given back out of a payment: a cancellation or a refund.
/// </summary>
public sealed record GiveBack : Payment
{
    internal GiveBack(JsonElement body) : base(body)
    {
        var refund = body.Field("refund");
        Type = Read.String(refund.Field("type"));
        Amount = Read.OptionalString(refund.Field("amount"));
    }

    /// <summary>Which of the two it was: a cancellation or a refund.</summary>
    public string Type { get; }

    /// <summary>How much actually went back, whether or not it was asked for by name.</summary>
    public string? Amount { get; }
}

/// <summary>
/// An order opened to be paid on the gateway's own page.
/// </summary>
public sealed record OrderPayment
{
    internal OrderPayment(JsonElement body)
    {
        var order = body.Field("order");

        Result = new Result(body);
        Token = Read.String(order.Field("token"));
        ChannelToken = Read.String(order.Field("channel_token"));
        ChannelReference = Read.String(order.Field("channel_reference"));
        Amount = Read.String(order.Field("amount"));
        Currency = Read.String(order.Field("currency"));
        Status = Read.String(order.Field("status"));
        CheckoutUrl = Read.String(order.Field("checkout_url"));
        CustomerChannelReference = Read.String(body.Field("customer").Field("channel_reference"));
    }

    public Result Result { get; }

    /// <summary>The order's token in the gateway.</summary>
    public string Token { get; }

    /// <summary>The channel the order was opened on.</summary>
    public string ChannelToken { get; }

    /// <summary>The number the order is known by in the calling system.</summary>
    public string ChannelReference { get; }

    /// <summary>What the order comes to, added up from its lines by the gateway.</summary>
    public string Amount { get; }

    public string Currency { get; }

    /// <summary>Where the order stands: open until it is paid.</summary>
    public string Status { get; }

    /// <summary>Where the customer has to be sent to pay.</summary>
    public string CheckoutUrl { get; }

    /// <summary>The merchant's own key for the customer the order is for.</summary>
    public string CustomerChannelReference { get; }
}

/// <summary>
/// A product in the merchant's catalogue at the gateway.
/// </summary>
public sealed record Product
{
    internal Product(JsonElement body)
    {
        var product = body.Field("product");

        Result = new Result(body);
        ChannelToken = Read.String(product.Field("channel_token"));
        ChannelReference = Read.String(product.Field("channel_reference"));
        Name = Read.String(product.Field("name"));
        Image = Read.OptionalString(product.Field("image"));
        Type = Read.String(product.Field("type"));
        Amount = Read.String(product.Field("amount"));
        Currency = Read.String(product.Field("currency"));
        TaxRate = Read.String(product.Field("tax_rate"));
        Period = Read.OptionalString(product.Field("period"));
        IsActive = Read.Bool(product.Field("is_active"));
    }

    public Result Result { get; }

    /// <summary>The channel the product is sold on.</summary>
    public string ChannelToken { get; }

    /// <summary>The key the product is known by in the calling system.</summary>
    public string ChannelReference { get; }

    public string Name { get; }

    /// <summary>The address of the picture the checkout shows it with; null when it has none.</summary>
    public string? Image { get; }

    /// <summary>simple or recurring.</summary>
    public string Type { get; }

    /// <summary>The price of one, as digits with the kurus behind a point.</summary>
    public string Amount { get; }

    public string Currency { get; }

    /// <summary>The tax included in the price, as a percentage.</summary>
    public string TaxRate { get; }

    /// <summary>monthly or annually for a recurring product; null for a simple one.</summary>
    public string? Period { get; }

    /// <summary>Whether it is on sale.</summary>
    public bool IsActive { get; }
}
