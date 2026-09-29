using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// The customer a payment is made for, an order is opened for or a card is
/// kept for. The merchant names them by its own key for them on the channel
/// they came in on: the same key twice is the same customer, and what is said
/// of them here becomes the latest the gateway knows.
/// </summary>
public sealed class Customer
{
    /// <summary>The key the merchant keeps this customer under in its own system.</summary>
    public required string ChannelReference { get; init; }

    public required string Firstname { get; init; }

    public required string Lastname { get; init; }

    public required string Email { get; init; }

    public required string Phone { get; init; }

    public required string Address { get; init; }

    public required string District { get; init; }

    public required string Province { get; init; }

    public required string Country { get; init; }

    /// <summary>The company they are billed as, for a customer buying for one.</summary>
    public TaxDetails? Tax { get; init; }

    internal JsonObject ToBody()
    {
        var body = Fields.Of(
            ("channel_reference", ChannelReference),
            ("firstname", Firstname),
            ("lastname", Lastname),
            ("email", Email),
            ("phone", Phone),
            ("address", Address),
            ("district", District),
            ("province", Province),
            ("country", Country));

        if (Tax is not null)
        {
            body["tax"] = Tax.ToBody();
        }

        return body;
    }
}

/// <summary>
/// Who a customer is billed as when they buy for a company. The three are
/// always given together: the gateway turns down a customer that names one of
/// them without the others.
/// </summary>
public sealed class TaxDetails
{
    public required string CompanyTitle { get; init; }

    public required string TaxNumber { get; init; }

    public required string TaxOffice { get; init; }

    internal JsonObject ToBody()
    {
        return Fields.Of(
            ("company_title", CompanyTitle),
            ("tax_number", TaxNumber),
            ("tax_office", TaxOffice));
    }
}

/// <summary>
/// A customer the gateway already knows, named and nothing more: the channel
/// they came in on and the merchant's own key for them there. It is what the
/// endpoints that only look a customer up take, such as listing the cards
/// kept for them.
/// </summary>
public sealed class NamedCustomer
{
    public required string ChannelReference { get; init; }

    internal JsonObject ToBody(string channelToken)
    {
        return Fields.Of(
            ("channel_token", channelToken),
            ("channel_reference", ChannelReference));
    }
}
