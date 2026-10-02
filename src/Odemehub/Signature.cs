using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Odemehub;

/// <summary>
/// How a merchant and the gateway vouch for each other's messages. A body
/// travels as plain JSON and, next to it, two headers: the moment it was
/// signed, in Unix seconds (<c>X-Timestamp</c>), and an HMAC-SHA256 under the
/// merchant's secret over that moment, the HTTP method, the path and the
/// exact text of the body, joined with newlines (<c>X-Signature</c>). The
/// secret itself never travels; a message whose signature does not match was
/// not written by the holder of the secret, or was changed on the way, and
/// one signed more than a few minutes ago is not taken either.
/// </summary>
/// <remarks>
/// A request and its answer are signed the same way, so one calculation
/// checks both:
///
/// <c>HMAC-SHA256(apiSecret, "{timestamp}\n{METHOD}\n{path}\n{body}")</c>, lowercase hex.
///
/// The path is the one in the address, with its leading slash and without
/// the query string; the body is the raw text as sent, and the empty string
/// for a GET.
///
/// Test vector: with the secret <c>secret_test</c>, at <c>1700000000</c>, a
/// <c>POST</c> to <c>/api/1000000001/gateway/regular-payment</c> with the body
/// <c>{"a":1}</c> is signed
/// <c>4d6225c9dd46837418b40dd8140d76a24cd7520d81ff3b280bf98da8da6a8771</c>.
/// </remarks>
public sealed class Signature
{
    /// <summary>The header both the request and the answer carry the signature in.</summary>
    public const string Header = "X-Signature";

    /// <summary>The header both the request and the answer carry the moment of signing in.</summary>
    public const string TimestampHeader = "X-Timestamp";

    /// <summary>
    /// How far from now, either way, a signature's moment may lie and still be
    /// taken, in seconds. The gateway allows the same.
    /// </summary>
    public const int TimestampTolerance = 300;

    private readonly byte[] _key;

    public Signature(string apiSecret)
    {
        _key = Encoding.UTF8.GetBytes(apiSecret);
    }

    /// <summary>
    /// The signature that vouches for a message: HMAC-SHA256 over the moment,
    /// the method, the path and the exact bytes of the body, written as
    /// lowercase hex.
    /// </summary>
    public string Sign(string method, string path, ReadOnlySpan<byte> body, long timestamp)
    {
        return Hmac(SignedText(method, path, body, timestamp));
    }

    public string Sign(string method, string path, string body, long timestamp)
    {
        return Sign(method, path, Encoding.UTF8.GetBytes(body), timestamp);
    }

    /// <summary>
    /// Whether a signature vouches for a message, and was made recently
    /// enough to be taken.
    /// </summary>
    /// <param name="method">The HTTP method of the request.</param>
    /// <param name="path">The path of the request, with its leading slash and without the query string.</param>
    /// <param name="body">The body, byte for byte as it travelled.</param>
    /// <param name="timestamp">The <c>X-Timestamp</c> header, as it arrived.</param>
    /// <param name="signature">The <c>X-Signature</c> header, as it arrived.</param>
    public bool Verify(string method, string path, ReadOnlySpan<byte> body, string? timestamp, string? signature)
    {
        if (signature is null
            || !long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var moment)
            || Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - moment) > TimestampTolerance)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Sign(method, path, body, moment)),
            Encoding.ASCII.GetBytes(signature));
    }

    public bool Verify(string method, string path, string body, string? timestamp, string? signature)
    {
        return Verify(method, path, Encoding.UTF8.GetBytes(body), timestamp, signature);
    }

    /// <summary>
    /// HMAC-SHA256 over the text, written as lowercase hex.
    /// </summary>
    private string Hmac(ReadOnlySpan<byte> text)
    {
        return Convert.ToHexString(HMACSHA256.HashData(_key, text)).ToLowerInvariant();
    }

    /// <summary>
    /// What the signature is taken over: the moment, the method, the path and
    /// the body, each on its own line.
    /// </summary>
    private static byte[] SignedText(string method, string path, ReadOnlySpan<byte> body, long timestamp)
    {
        var head = Encoding.UTF8.GetBytes($"{timestamp.ToString(CultureInfo.InvariantCulture)}\n{method.ToUpperInvariant()}\n{path}\n");
        var text = new byte[head.Length + body.Length];

        head.CopyTo(text, 0);
        body.CopyTo(text.AsSpan(head.Length));

        return text;
    }

    public override string ToString()
    {
        return "Signature { ApiSecret = ***** }";
    }
}
