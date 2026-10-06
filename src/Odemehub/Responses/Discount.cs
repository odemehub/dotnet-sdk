using System.Text.Json;

namespace Odemehub.Responses;

/// <summary>
/// The coupon the payer put on an order, a subscription's first payment or a
/// payment at a link on the checkout page: the code they typed and what it
/// took off the lines. Coupons are never sent through the gateway; they are
/// only told of.
/// </summary>
public sealed record Discount
{
    internal Discount(JsonElement discount)
    {
        Code = Read.String(discount.Field("code"));
        Amount = Read.String(discount.Field("amount"));
    }

    /// <summary>The code the payer typed.</summary>
    public string Code { get; }

    /// <summary>What it took off the lines, in the thing's own money, with the kurus behind a point.</summary>
    public string Amount { get; }

    /// <summary>The coupon a thing carries; null for one no coupon was put on.</summary>
    internal static Discount? Of(JsonElement discount)
    {
        return discount.ValueKind == JsonValueKind.Object ? new Discount(discount) : null;
    }
}
