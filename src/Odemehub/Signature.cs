using System;
using System.Security.Cryptography;
using System.Text;

namespace Odemehub;

/// <summary>
/// How a merchant and the gateway vouch for each other's bodies. A body
/// travels as plain JSON and, next to it in the <c>X-Signature</c> header, an
/// HMAC-SHA256 of that exact text under the merchant's secret. The secret
/// itself never travels; a body whose signature does not match was not
/// written by the holder of the secret, or was changed on the way.
/// </summary>
/// <remarks>
/// This is the same calculation the application makes in its own
/// <c>Services\Gateway\Signer</c>. Nothing is layered on top, so a body can be
/// signed and checked by hand with any HMAC-SHA256.
///
/// Test vector: with the secret <c>secret_test</c> the body <c>{"a":1}</c> is
/// signed <c>6d0c951564cdd2b6b70e75b214293a8cd2542815ba54fe91c7f6ce105bc3d592</c>.
/// </remarks>
public sealed class Signature
{
    /// <summary>The header both the request and the answer carry the signature in.</summary>
    public const string Header = "X-Signature";

    private readonly byte[] _key;

    public Signature(string apiSecret)
    {
        _key = Encoding.UTF8.GetBytes(apiSecret);
    }

    /// <summary>
    /// The signature that vouches for a body: HMAC-SHA256 over the exact bytes,
    /// written as lowercase hex.
    /// </summary>
    public string Sign(ReadOnlySpan<byte> body)
    {
        return Convert.ToHexString(HMACSHA256.HashData(_key, body)).ToLowerInvariant();
    }

    public string Sign(string body)
    {
        return Sign(Encoding.UTF8.GetBytes(body));
    }

    /// <summary>
    /// Whether a signature vouches for a body.
    /// </summary>
    public bool Verify(ReadOnlySpan<byte> body, string? signature)
    {
        return signature is not null
            && CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(Sign(body)),
                Encoding.ASCII.GetBytes(signature));
    }

    public bool Verify(string body, string? signature)
    {
        return Verify(Encoding.UTF8.GetBytes(body), signature);
    }

    public override string ToString()
    {
        return "Signature { ApiSecret = ***** }";
    }
}
