using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// The customer a payment is made for, an order or a subscription is opened
/// for, or a card is kept for: the merchant's own key for them, where they
/// are billed and, for goods, where those go.
/// </summary>
/// <remarks>
/// The reference is what makes them one of the team's customers: the customer
/// is written under it once a payment for them goes through, and their cards
/// are kept for them and found again by it. It may be left out of a payment
/// or an order, and the payer is then nobody the team keeps; but a payment
/// that keeps its card, a card kept on its own and a subscription have to
/// carry it.
///
/// Payments and kept cards take the reference and the billing address only,
/// the billing address whole; orders and subscriptions take any part of the
/// three, and the checkout page asks the payer for the rest.
/// </remarks>
public sealed class Customer
{
    /// <summary>The key the merchant keeps this customer under in its own system.</summary>
    public string? Reference { get; init; }

    /// <summary>Where the bill goes: the person and, when they buy for one, the company.</summary>
    public Address? BillingAddress { get; init; }

    /// <summary>Where the goods go. Orders and subscriptions only.</summary>
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
/// Where a customer is billed, or where their goods go. A payment and a kept
/// card need the whole of the eight person fields on the billing address; an
/// order or a subscription takes whatever is known.
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
