using System.Text.Json;

namespace Odemehub.Responses;

/// <summary>
/// Where a customer is billed, or where their goods go, as it was written:
/// the fields given, and null for the ones never said.
/// </summary>
public sealed record Address
{
    internal Address(JsonElement address)
    {
        Firstname = Read.NonEmptyString(address.Field("firstname"));
        Lastname = Read.NonEmptyString(address.Field("lastname"));
        Email = Read.NonEmptyString(address.Field("email"));
        Phone = Read.NonEmptyString(address.Field("phone"));
        AddressLine = Read.NonEmptyString(address.Field("address"));
        District = Read.NonEmptyString(address.Field("district"));
        Province = Read.NonEmptyString(address.Field("province"));
        Country = Read.NonEmptyString(address.Field("country"));
        CompanyTitle = Read.NonEmptyString(address.Field("company_title"));
        TaxNumber = Read.NonEmptyString(address.Field("tax_number"));
        TaxOffice = Read.NonEmptyString(address.Field("tax_office"));
    }

    public string? Firstname { get; }

    public string? Lastname { get; }

    public string? Email { get; }

    public string? Phone { get; }

    /// <summary>The street address, read from <c>address</c>.</summary>
    public string? AddressLine { get; }

    public string? District { get; }

    public string? Province { get; }

    public string? Country { get; }

    /// <summary>The company the customer is billed as; billing address only.</summary>
    public string? CompanyTitle { get; }

    public string? TaxNumber { get; }

    public string? TaxOffice { get; }
}

/// <summary>
/// The customer a payment was made for, as the payment wrote them down: its
/// own copy, which stays as it was however the thing paid for moves on.
/// </summary>
public sealed record PaymentCustomer
{
    internal PaymentCustomer(JsonElement customer)
    {
        Reference = Read.NonEmptyString(customer.Field("reference"));
        BillingAddress = new Address(customer.Field("billing_address"));
    }

    /// <summary>The merchant's own key for the customer; null for a payer nobody keeps a key for.</summary>
    public string? Reference { get; }

    public Address BillingAddress { get; }
}

/// <summary>
/// The customer of an order or a subscription, as they were written: the key
/// the merchant keeps them under — or the one the gateway made up for a payer
/// the merchant never named, <c>guest-…</c> — where the bill goes, and where
/// the goods go when somebody said.
/// </summary>
public sealed record NamedCustomer
{
    internal NamedCustomer(JsonElement customer)
    {
        var shipping = customer.Field("shipping_address");

        Reference = Read.String(customer.Field("reference"));
        BillingAddress = new Address(customer.Field("billing_address"));
        ShippingAddress = shipping.ValueKind == JsonValueKind.Object ? new Address(shipping) : null;
    }

    public string Reference { get; }

    public Address BillingAddress { get; }

    /// <summary>Where the goods go; null when nobody said.</summary>
    public Address? ShippingAddress { get; }

    /// <summary>Whether the reference is one the gateway made up for a payer the merchant never named.</summary>
    public bool IsGuest => Reference.StartsWith("guest-", System.StringComparison.Ordinal);
}
