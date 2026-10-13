using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// The customer an order or a subscription is opened for: the merchant's own
/// key for them, where they are billed and, for goods, where those go.
/// Payments and kept cards take a <see cref="PaymentCustomer"/> instead.
/// </summary>
/// <remarks>
/// The reference is what makes them one of the team's customers: the customer
/// is written under it once a payment for them goes through, and their cards
/// are kept for them and found again by it. It may be left out of an order;
/// a subscription has to carry it. Any part of the three may be given, and
/// the checkout page asks the payer for the rest.
/// </remarks>
public sealed class Customer
{
    /// <summary>The key the merchant keeps this customer under in its own system.</summary>
    public string? Reference { get; init; }

    /// <summary>Where the bill goes: the person and, when they buy for one, the company.</summary>
    public Address? BillingAddress { get; init; }

    /// <summary>Where the goods go.</summary>
    public Address? ShippingAddress { get; init; }

    internal JsonObject ToBody()
    {
        return Fields.Said(
            ("reference", Reference),
            ("billing_address", BillingAddress?.ToBody()),
            ("shipping_address", ShippingAddress?.ToBody()));
    }
}

/// <summary>
/// The customer a payment is made for or a card is kept for: the whole
/// billing address, or what is known of it where the team fills the rest
/// from its default payer (Varsayılan Ödeyen), and the merchant's own key
/// for them. Nothing goes to a
/// shipping address here; the gateway turns down a payment or a kept card
/// that names one.
/// </summary>
/// <remarks>
/// The reference may be left out of a payment, and the payer is then nobody
/// the team keeps; but a payment that keeps its card, one made with a kept
/// card and a card kept on its own have to carry it.
/// </remarks>
public sealed class PaymentCustomer
{
    /// <summary>The key the merchant keeps this customer under in its own system.</summary>
    public string? Reference { get; init; }

    /// <summary>Where the bill goes: all eight person fields, unless the team fills what is left out from its default payer, and, when they buy for one, the three company fields together.</summary>
    public required Address BillingAddress { get; init; }

    internal JsonObject ToBody()
    {
        return Fields.Said(
            ("reference", Reference),
            ("billing_address", BillingAddress.ToBody()));
    }
}

/// <summary>
/// Where a customer is billed, or where their goods go. A payment and a kept
/// card need the whole of the eight person fields on the billing address,
/// unless the team fills what is left out from its default payer (Varsayılan
/// Ödeyen); an order or a subscription takes whatever is known.
/// </summary>
/// <remarks>
/// The three company fields are read on the billing address only, and always
/// together: the gateway turns down an address that names one of them without
/// the others.
/// </remarks>
public sealed class Address
{
    public string? Firstname { get; init; }

    public string? Lastname { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    /// <summary>
    /// The street address — neighbourhood, street, building, door — sent as
    /// <c>address</c>. A C# property cannot carry the name of the class it is
    /// in, hence the longer name.
    /// </summary>
    public string? AddressLine { get; init; }

    public string? District { get; init; }

    public string? Province { get; init; }

    public string? Country { get; init; }

    /// <summary>The company the customer is billed as. Billing address only.</summary>
    public string? CompanyTitle { get; init; }

    public string? TaxNumber { get; init; }

    public string? TaxOffice { get; init; }

    internal JsonObject ToBody()
    {
        return Fields.Said(
            ("firstname", Firstname),
            ("lastname", Lastname),
            ("email", Email),
            ("phone", Phone),
            ("address", AddressLine),
            ("district", District),
            ("province", Province),
            ("country", Country),
            ("company_title", CompanyTitle),
            ("tax_number", TaxNumber),
            ("tax_office", TaxOffice));
    }
}
