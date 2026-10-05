using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// Something handed to the gateway. Everything sent there is plain JSON,
/// posted and signed as a whole by the client, so what is common to all of
/// them is the endpoint it goes to and the body it is sent as.
/// </summary>
public abstract class Message
{
    /// <summary>
    /// The endpoint this is sent to, under the team's gateway, with the token
    /// in the address where the endpoint takes one.
    /// </summary>
    internal abstract string Path { get; }

    /// <summary>
    /// The HTTP method this goes with: every endpoint is posted to.
    /// </summary>
    internal string Method => "POST";

    /// <summary>
    /// The request body, in the snake_case the gateway speaks.
    /// </summary>
    internal abstract JsonObject ToBody();
}


/// <summary>
/// Money given back out of a payment that has already been made. The payment
/// is named by the token the gateway gave it, and nothing else is sent: the
/// gateway holds the account, the provider and the reference the provider
/// knows the payment by.
/// </summary>
public abstract class PaymentMessage : Message
{
    /// <summary>The payment's token in the gateway, as it answered when the payment was made.</summary>
    public required string Token { get; init; }

    internal override JsonObject ToBody()
    {
        return Fields.Of(("transaction", Fields.Of(("token", Token))));
    }
}

/// <summary>
/// Asking after records of one kind. They are named one of three ways: by the
/// token the gateway gave one, by the merchant's own reference for them, or by
/// the days they were made on, as <c>YYYY-MM-DD</c> in the team's own
/// timezone, both ends included and at most seven days apart. Asked with none
/// of these, it is the last seven days up to today. The answer is always a
/// list, oldest first, and an empty one when nothing matches. Nothing is
/// changed by asking.
/// </summary>
public abstract class Retrieve : Message
{
    /// <summary>The record's token in the gateway.</summary>
    public string? Token { get; init; }

    /// <summary>The merchant's own reference for them.</summary>
    public string? Reference { get; init; }

    /// <summary>The first day, as <c>YYYY-MM-DD</c>. Given together with <see cref="CreatedTo"/>.</summary>
    public string? CreatedFrom { get; init; }

    /// <summary>The last day, as <c>YYYY-MM-DD</c>, at most six days after the first.</summary>
    public string? CreatedTo { get; init; }

    /// <summary>The field the merchant's own reference travels in.</summary>
    internal virtual string ReferenceField => "reference";

    internal override JsonObject ToBody()
    {
        return Fields.Said(
            ("token", Token),
            (ReferenceField, Reference),
            ("created_from", CreatedFrom),
            ("created_to", CreatedTo));
    }
}
