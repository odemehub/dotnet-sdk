using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Odemehub.Requests;

/// <summary>
/// The banks whose accounts may be added, each with the credentials it asks
/// for. Nothing is sent but the signature.
/// </summary>
public sealed class RetrieveBanks : Message
{
    internal override string Path => "retrieve-banks";

    internal override JsonObject ToBody()
    {
        return Fields.Of();
    }
}

/// <summary>
/// A bank account whose movements are to be read. The gateway first asks the
/// bank whether the credentials read the account with the IBAN; one the bank
/// turns down is not added, and the answer says why. An added account is named
/// by the token the gateway gives it from then on.
/// </summary>
/// <remarks>
/// The gateway reads every account from the bank within the hour, the last
/// seven days the first time; the movements are asked after with
/// <see cref="RetrieveBankTransactions"/>. The credentials never come back in
/// any answer.
/// </remarks>
public sealed class CreateBankAccount : Message
{
    /// <summary>The bank's id, as <see cref="RetrieveBanks"/> lists it, e.g. <c>enpara</c>.</summary>
    public required string Provider { get; init; }

    /// <summary>What the account is called in the panel and the listing.</summary>
    public required string Name { get; init; }

    /// <summary>The account's IBAN; spaces and small letters are fine. A team adds an IBAN once.</summary>
    public required string Iban { get; init; }

    /// <summary>Every credential the bank asks for, keyed the way <see cref="RetrieveBanks"/> lists them.</summary>
    public required IReadOnlyDictionary<string, string> Settings { get; init; }

    internal override string Path => "create-bank-account";

    internal override JsonObject ToBody()
    {
        return Fields.Of(("bank_account", Fields.Of(
            ("provider", Provider),
            ("name", Name),
            ("iban", Iban),
            ("settings", Fields.Map(Settings)))));
    }
}

/// <summary>
/// The team's bank accounts asked after: the ones named by their tokens, or
/// every one of them when none is named, oldest first. A token that is not
/// the team's finds nothing.
/// </summary>
public sealed class RetrieveBankAccounts : Message
{
    /// <summary>The accounts' tokens in the gateway, at most fifty. Left out, every account of the team's.</summary>
    public IReadOnlyList<string>? Tokens { get; init; }

    internal override string Path => "retrieve-bank-accounts";

    internal override JsonObject ToBody()
    {
        return Fields.Said(("tokens", Fields.List(Tokens)));
    }
}

/// <summary>
/// A change to a bank account, named by its token in the address and again in
/// the body: its name, and the credentials given, the rest staying as they
/// are. New credentials are tried against the bank first and kept only if it
/// lets them read the account. Its bank and its IBAN do not change. A change
/// that sends neither a name nor credentials is turned down (422).
/// </summary>
public sealed class UpdateBankAccount : Message
{
    /// <summary>The account's token in the gateway.</summary>
    public required string Token { get; init; }

    public string? Name { get; init; }

    /// <summary>Only the credentials to replace, keyed the way <see cref="RetrieveBanks"/> lists them.</summary>
    public IReadOnlyDictionary<string, string>? Settings { get; init; }

    internal override string Path => $"update-bank-account/{Token}";

    internal override JsonObject ToBody()
    {
        return Fields.Of(
            ("token", Token),
            ("bank_account", Fields.Said(
                ("name", Name),
                ("settings", Fields.Map(Settings)))));
    }
}

/// <summary>
/// A bank account to be removed, named by its token in the address and again
/// in the body. Every movement read off it goes with it, and so do its
/// credentials, unless another account is read with them.
/// </summary>
public sealed class DeleteBankAccount : Message
{
    /// <summary>The account's token in the gateway.</summary>
    public required string Token { get; init; }

    internal override string Path => $"delete-bank-account/{Token}";

    internal override JsonObject ToBody()
    {
        return Fields.Of(("token", Token));
    }
}

/// <summary>
/// The movements on one or more bank accounts, named by their tokens, booked
/// between two days as <c>YYYY-MM-DD</c> in the team's own timezone, both ends
/// included and at most seven days apart; the last seven days when none are
/// given. The days are the bank's: the day it booked the movement, not the day
/// the gateway read it.
/// </summary>
/// <remarks>
/// The answer comes a page at a time, oldest first, the accounts' movements
/// woven together by when the bank booked them. They are read from what the
/// gateway keeps, which it reads from the bank within the hour.
/// </remarks>
public sealed class RetrieveBankTransactions : Message
{
    /// <summary>The accounts' tokens in the gateway, one to fifty.</summary>
    public required IReadOnlyList<string> BankAccountTokens { get; init; }

    /// <summary>The first day, as <c>YYYY-MM-DD</c>. Given together with <see cref="CreatedTo"/>.</summary>
    public string? CreatedFrom { get; init; }

    /// <summary>The last day, as <c>YYYY-MM-DD</c>, at most six days after the first.</summary>
    public string? CreatedTo { get; init; }

    /// <summary>The page, from 1. Left out, the first.</summary>
    public int? Page { get; init; }

    /// <summary>How many movements a page holds, at most 250. Left out, 50 when <see cref="Page"/> is given and 250 when neither is.</summary>
    public int? PerPage { get; init; }

    internal override string Path => "retrieve-bank-transactions";

    internal override JsonObject ToBody()
    {
        return Fields.Said(
            ("bank_account_tokens", Fields.List(BankAccountTokens)),
            ("created_from", CreatedFrom),
            ("created_to", CreatedTo),
            ("page", Page),
            ("per_page", PerPage));
    }
}
