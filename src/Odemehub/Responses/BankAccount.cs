using System.Collections.Generic;
using System.Text.Json;
using Odemehub.Enums;

namespace Odemehub.Responses;

/// <summary>
/// A bank whose accounts may be added, with the credentials it asks for.
/// </summary>
public sealed record Bank
{
    internal Bank(JsonElement bank)
    {
        Id = Read.String(bank.Field("id"));
        Title = Read.String(bank.Field("title"));
        Fields = Read.List(bank.Field("fields"), field => new BankField(field));
    }

    /// <summary>What the bank is named by when an account is added, e.g. <c>enpara</c>.</summary>
    public string Id { get; }

    public string Title { get; }

    /// <summary>The credentials an account at the bank is added with.</summary>
    public IReadOnlyList<BankField> Fields { get; }
}

/// <summary>
/// One credential a bank asks for.
/// </summary>
public sealed record BankField
{
    internal BankField(JsonElement field)
    {
        Key = Read.String(field.Field("key"));
        IsRequired = Read.Bool(field.Field("required"));
        Label = Read.String(field.Field("label"));
    }

    /// <summary>The key it is sent under in <c>Settings</c>.</summary>
    public string Key { get; }

    public bool IsRequired { get; }

    /// <summary>What the bank calls it, to show beside the input.</summary>
    public string Label { get; }
}

/// <summary>
/// The banks whose accounts may be added.
/// </summary>
public sealed record BankList
{
    internal BankList(JsonElement body)
    {
        Result = new Result(body);
        Banks = Read.List(body.Field("banks"), bank => new Bank(bank));
    }

    public Result Result { get; }

    public IReadOnlyList<Bank> Banks { get; }
}

/// <summary>
/// A bank account whose movements the gateway reads: enough to name it again
/// by its token and to tell how its reading went, and never its credentials.
/// </summary>
public sealed record BankAccount
{
    internal BankAccount(JsonElement bankAccount)
    {
        Token = Read.String(bankAccount.Field("token"));
        Provider = Read.String(bankAccount.Field("provider"));
        Name = Read.String(bankAccount.Field("name"));
        Iban = Read.String(bankAccount.Field("iban"));
        SyncedAt = Read.NonEmptyString(bankAccount.Field("synced_at"));
        LastError = Read.NonEmptyString(bankAccount.Field("last_error"));
        CreatedAt = Read.NonEmptyString(bankAccount.Field("created_at"));
    }

    /// <summary>The account's token in the gateway, which its movements are asked for by.</summary>
    public string Token { get; }

    /// <summary>The bank's id, e.g. <c>enpara</c>.</summary>
    public string Provider { get; }

    public string Name { get; }

    /// <summary>The IBAN, in capitals without spaces.</summary>
    public string Iban { get; }

    /// <summary>When the account was last read from the bank, ISO 8601 UTC; null until it has been.</summary>
    public string? SyncedAt { get; }

    /// <summary>Why the last reading failed, in the bank's words; null when it went through.</summary>
    public string? LastError { get; }

    public string? CreatedAt { get; }
}

/// <summary>
/// A bank account added or changed.
/// </summary>
public sealed record BankAccountDetails
{
    internal BankAccountDetails(JsonElement body)
    {
        var bankAccount = body.Field("bank_account");

        Result = new Result(body);
        BankAccount = bankAccount.ValueKind == JsonValueKind.Object ? new BankAccount(bankAccount) : null;
    }

    /// <summary>Not successful when the bank turned the credentials down; the message is the bank's.</summary>
    public Result Result { get; }

    /// <summary>The account as it now stands; null when the bank turned the credentials down and nothing changed.</summary>
    public BankAccount? BankAccount { get; }
}

/// <summary>
/// The team's bank accounts asked after, oldest first; an empty list when
/// nothing matched.
/// </summary>
public sealed record BankAccountList
{
    internal BankAccountList(JsonElement body)
    {
        Result = new Result(body);
        BankAccounts = Read.List(body.Field("bank_accounts"), entry => new BankAccount(entry));
    }

    public Result Result { get; }

    public IReadOnlyList<BankAccount> BankAccounts { get; }
}

/// <summary>
/// A bank account removed, with every movement read off it.
/// </summary>
public sealed record DeletedBankAccount
{
    internal DeletedBankAccount(JsonElement body)
    {
        Result = new Result(body);
        BankAccountToken = Read.String(body.Field("bank_account").Field("token"));
    }

    public Result Result { get; }

    /// <summary>The token of the account that was removed.</summary>
    public string BankAccountToken { get; }
}

/// <summary>
/// A movement on a bank account, as the bank booked it.
/// </summary>
public sealed record BankTransaction
{
    internal BankTransaction(JsonElement bankTransaction)
    {
        Token = Read.String(bankTransaction.Field("token"));
        BankAccountToken = Read.String(bankTransaction.Field("bank_account_token"));
        BookedAt = Read.String(bankTransaction.Field("booked_at"));
        ValueDate = Read.NonEmptyString(bankTransaction.Field("value_date"));
        Amount = Read.String(bankTransaction.Field("amount"));
        Currency = Read.Enum<Currency>(bankTransaction.Field("currency"));
        BalanceAfter = Read.OptionalString(bankTransaction.Field("balance_after"));
        Description = Read.NonEmptyString(bankTransaction.Field("description"));
        SenderDescription = Read.NonEmptyString(bankTransaction.Field("sender_description"));
        CounterpartyIban = Read.NonEmptyString(bankTransaction.Field("counterparty_iban"));
        CounterpartyTaxNumber = Read.NonEmptyString(bankTransaction.Field("counterparty_tax_number"));
    }

    /// <summary>The movement's token in the gateway.</summary>
    public string Token { get; }

    /// <summary>The account it was read off.</summary>
    public string BankAccountToken { get; }

    /// <summary>When the bank booked it, ISO 8601 UTC.</summary>
    public string BookedAt { get; }

    /// <summary>The day the money counts from, as <c>YYYY-MM-DD</c>; null when the bank does not say.</summary>
    public string? ValueDate { get; }

    /// <summary>As digits with the kurus behind a point, signed: above zero coming in, below zero going out, e.g. <c>"-40.00"</c>.</summary>
    public string Amount { get; }

    public Currency Currency { get; }

    /// <summary>What the account held after it, the same way; null when the bank does not say.</summary>
    public string? BalanceAfter { get; }

    /// <summary>The bank's own description of it.</summary>
    public string? Description { get; }

    /// <summary>What the sender wrote on it.</summary>
    public string? SenderDescription { get; }

    /// <summary>The IBAN on the other side.</summary>
    public string? CounterpartyIban { get; }

    /// <summary>The tax or identity number on the other side.</summary>
    public string? CounterpartyTaxNumber { get; }
}

/// <summary>
/// Where a page stands among all the records that matched.
/// </summary>
public sealed record Pagination
{
    internal Pagination(JsonElement pagination)
    {
        Page = Read.Int(pagination.Field("page"));
        PerPage = Read.Int(pagination.Field("per_page"));
        Total = Read.Int(pagination.Field("total"));
        LastPage = Read.Int(pagination.Field("last_page"));
    }

    /// <summary>This page, from 1.</summary>
    public int Page { get; }

    public int PerPage { get; }

    /// <summary>How many records matched across every page.</summary>
    public int Total { get; }

    /// <summary>The last page there is; the same as <see cref="Page"/> on the last one.</summary>
    public int LastPage { get; }

    /// <summary>
    /// Where a page stands, or nothing for an answer that leaves it out.
    /// </summary>
    internal static Pagination? Of(JsonElement pagination)
    {
        return pagination.ValueKind == JsonValueKind.Object ? new Pagination(pagination) : null;
    }
}

/// <summary>
/// A page of the movements on the accounts asked after, oldest first, the
/// accounts' movements woven together by when the bank booked them; an empty
/// list when nothing matched. The days are the ones the gateway used: the ones
/// asked for, or the last seven when none were.
/// </summary>
public sealed record BankTransactionList
{
    internal BankTransactionList(JsonElement body)
    {
        Result = new Result(body);
        CreatedFrom = Read.NonEmptyString(body.Field("created_from"));
        CreatedTo = Read.NonEmptyString(body.Field("created_to"));
        Pagination = new Pagination(body.Field("pagination"));
        BankTransactions = Read.List(body.Field("bank_transactions"), entry => new BankTransaction(entry));
    }

    public Result Result { get; }

    /// <summary>The first day listed, as <c>YYYY-MM-DD</c> in the team's timezone.</summary>
    public string? CreatedFrom { get; }

    /// <summary>The last day listed, the same way.</summary>
    public string? CreatedTo { get; }

    public Pagination Pagination { get; }

    public IReadOnlyList<BankTransaction> BankTransactions { get; }
}
