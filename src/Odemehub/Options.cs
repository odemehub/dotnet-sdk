using System;

namespace Odemehub;

/// <summary>
/// The address the gateway is reached at, the credentials it is reached with
/// and the channel the caller speaks for. A credential pair belongs to a
/// single team, and the team is part of the address, so a pair only ever
/// opens its own team's endpoints.
/// </summary>
public sealed class Options
{
    /// <summary>The header the API key travels in.</summary>
    public const string ApiKeyHeader = "X-Api-Key";

    /// <summary>The address the application is served from, e.g. https://app.odemehub.com.</summary>
    public required string BaseUrl { get; init; }

    /// <summary>The team the payments are made on behalf of, as the Entegrasyon page names it.</summary>
    public required string Team { get; init; }

    /// <summary>
    /// The channel every request speaks for: the shop, the marketplace or the
    /// branch the customer reached the merchant through, by the token the
    /// team's own Kanallar page gives it. A merchant selling on more than one
    /// channel may still name another on a single request.
    /// </summary>
    public required string ChannelToken { get; init; }

    public required string ApiKey { get; init; }

    public required string ApiSecret { get; init; }

    /// <summary>How long a request may take. Left out, a minute.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The full address of a gateway endpoint for this team.
    /// </summary>
    public string Url(string path)
    {
        return $"{BaseUrl.TrimEnd('/')}/api/{Team}/gateway/{path}";
    }

    public override string ToString()
    {
        return $"Options {{ BaseUrl = {BaseUrl}, Team = {Team}, ChannelToken = {ChannelToken}, Timeout = {Timeout} }}";
    }
}
