using System.Collections.Generic;
using System.Text.Json;
using Odemehub.Enums;

namespace Odemehub.Responses;

/// <summary>
/// An order as the gateway keeps it: what is being paid for, what it comes
/// to, where it stands, whose it is and — once it is paid — the payment that
/// paid it. The same shape comes back whether the order has just been opened,
/// changed, asked after or listed.
/// </summary>
/// <remarks>
/// Nothing is charged when an order is opened: the customer has to be sent to
/// <see cref="CheckoutUrl"/> and gives their card there.
/// </remarks>
public sealed record Order
{
    /// <summary>An order as an answer carries it: under <c>order</c>, with the customer beside it.</summary>
    internal Order(JsonElement body) : this(body.Field("order"), body.Field("customer"))
    {
    }

    internal Order(JsonElement order, JsonElement customer)
    {
        var shippingMethod = order.Field("shipping_method");
        var transaction = order.Field("transaction");

        Token = Read.String(order.Field("token"));
        Reference = Read.String(order.Field("reference"));
        Description = Read.NonEmptyString(order.Field("description"));
        PaymentProviderToken = Read.NonEmptyString(order.Field("payment_provider_token"));
        Status = Read.Enum<OrderStatus>(order.Field("status"));
        Items = Read.List(order.Field("items"), item => new Item(item));
        ShippingMethod = shippingMethod.ValueKind == JsonValueKind.Object ? new ShippingMethod(shippingMethod) : null;
        Discount = Discount.Of(order.Field("discount"));
        Subtotal = Read.String(order.Field("subtotal"));
        ShippingAmount = Read.String(order.Field("shipping_amount"));
        TaxAmount = Read.String(order.Field("tax_amount"));
        Amount = Read.String(order.Field("amount"));
        Currency = Read.Enum<Currency>(order.Field("currency"));
        IsTest = Read.OptionalBool(order.Field("is_test"));
        CreatedAt = Read.NonEmptyString(order.Field("created_at"));
        CheckoutUrl = Read.NonEmptyString(order.Field("checkout_url"));
        Transaction = transaction.ValueKind == JsonValueKind.Object ? new TransactionReference(transaction) : null;
        Customer = customer.ValueKind == JsonValueKind.Object ? new NamedCustomer(customer) : null;
    }

    /// <summary>The order's token in the gateway; name it to ask after or change it later.</summary>
    public string Token { get; }


    /// <summary>The number the order is known by in the calling system.</summary>
    public string Reference { get; }

    public string? Description { get; }

    /// <summary>The account the order is paid through; null when the team's Gate rules and default account decide.</summary>
    public string? PaymentProviderToken { get; }

    /// <summary>Where the order stands: open until it is paid, then paid.</summary>
    public OrderStatus Status { get; }

    /// <summary>What the order is made up of.</summary>
    public IReadOnlyList<Item> Items { get; }


    /// <summary>The way the payer picked; null until they have, or when none was offered.</summary>
    public ShippingMethod? ShippingMethod { get; }

    /// <summary>The coupon the payer put on the order at checkout; null when none was.</summary>
    public Discount? Discount { get; }

    /// <summary>What the lines come to before tax, less the coupon.</summary>
    public string Subtotal { get; }

    /// <summary>What the picked way of sending comes to before tax.</summary>
    public string ShippingAmount { get; }

    /// <summary>The tax the lines and the sending carry, less the coupon.</summary>
    public string TaxAmount { get; }

    /// <summary>What the order comes to, added up by the gateway: the lines less the coupon, and the picked way of sending.</summary>
    public string Amount { get; }

    public Currency Currency { get; }

    /// <summary>Whether it was opened in the test environment.</summary>
    public bool? IsTest { get; }

    public string? CreatedAt { get; }

    /// <summary>Where the customer pays, while the order can still be paid; null once it is paid.</summary>
    public string? CheckoutUrl { get; }

    /// <summary>The payment that paid the order, which names it again for a refund; null while it is open.</summary>
    public TransactionReference? Transaction { get; }

    /// <summary>Who the order is for; null while nobody has said.</summary>
    public NamedCustomer? Customer { get; }

    /// <summary>Whether the order has been paid.</summary>
    public bool IsPaid => Status == OrderStatus.Paid;
}

/// <summary>
/// The payment that paid an order: enough to ask after it or give money back
/// out of it, and what has become of its money since.
/// </summary>
public sealed record TransactionReference
{
    internal TransactionReference(JsonElement transaction)
    {
        Token = Read.String(transaction.Field("token"));
        Reference = Read.String(transaction.Field("reference"));
        PaymentStatus = Read.OptionalEnum<PaymentStatus>(transaction.Field("payment_status"));
    }

    /// <summary>The payment's token in the gateway.</summary>
    public string Token { get; }


    /// <summary>The reference the payment was made under.</summary>
    public string Reference { get; }

    /// <summary>What became of the money: paid, cancelled, refunded, partially refunded.</summary>
    public PaymentStatus? PaymentStatus { get; }
}

/// <summary>
/// An order opened or changed. Whose it is is said beside the
/// order, as the answer says it, and on the order as well.
/// </summary>
public sealed record OrderDetails
{
    internal OrderDetails(JsonElement body)
    {
        Result = new Result(body);
        Order = new Order(body);
        Customer = Order.Customer;
    }

    public Result Result { get; }

    public Order Order { get; }

    /// <summary>Who the order is for; null while nobody has said. The same as <c>Order.Customer</c>.</summary>
    public NamedCustomer? Customer { get; }
}

/// <summary>
/// Orders asked after, each with its customer on <c>Order.Customer</c>. The answer is always a list, oldest first, and an empty one when
/// nothing matched. The days are the ones the gateway used, when the records
/// were asked for by the days they were made on: the ones asked for, or the
/// last seven when none were.
/// </summary>
public sealed record OrderList
{
    internal OrderList(JsonElement body)
    {
        Result = new Result(body);
        CreatedFrom = Read.NonEmptyString(body.Field("created_from"));
        CreatedTo = Read.NonEmptyString(body.Field("created_to"));
        Orders = Read.List(body.Field("orders"), entry => new Order(entry, entry.Field("customer")));
    }

    public Result Result { get; }

    /// <summary>The first day listed, as <c>YYYY-MM-DD</c> in the team's timezone; null when they were asked for by token or reference.</summary>
    public string? CreatedFrom { get; }

    /// <summary>The last day listed, the same way.</summary>
    public string? CreatedTo { get; }

    public IReadOnlyList<Order> Orders { get; }
}
