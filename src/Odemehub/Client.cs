using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Odemehub.Requests;
using Odemehub.Responses;

namespace Odemehub;

/// <summary>
/// The gateway, as the merchant's application talks to it. Every request
/// leaves signed with the team's secret and every answer is checked against
/// it, so both sides can tell the other is really who it says it is.
/// </summary>
/// <remarks>
/// There is one method per endpoint, named after it: <c>create-order</c> is
/// <c>CreateOrderAsync</c>, and takes a <c>Requests.CreateOrder</c>.
///
/// A client holds no state beyond its options, so one can be shared across
/// the application for its whole life, e.g. registered as a singleton.
/// </remarks>
public sealed class Client
{
    private static readonly HttpClient SharedHttp = new() { Timeout = System.Threading.Timeout.InfiniteTimeSpan };

    private readonly Options _options;
    private readonly HttpClient _http;
    private readonly Signature _signature;

    /// <param name="options">The address and the credentials.</param>
    /// <param name="http">
    /// The HTTP client the requests go through, for a merchant that already
    /// configures one (IHttpClientFactory, a proxy). Left out, one shared
    /// client is used.
    /// </param>
    public Client(Options options, HttpClient? http = null)
    {
        _options = options;
        _http = http ?? SharedHttp;
        _signature = new Signature(options.ApiSecret);
    }

    /// <summary>
    /// Start a payment the customer confirms with their bank. A successful
    /// answer is not a settled payment: the customer is still to be sent to
    /// the address it comes back with, and <see cref="RetrievePaymentsAsync"/>
    /// says what became of it once they are back.
    /// </summary>
    public async Task<Responses.SecurePayment> SecurePaymentAsync(Requests.SecurePayment payment, CancellationToken cancellationToken = default)
    {
        return new Responses.SecurePayment(await SendAsync(payment, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Charge a payment straight to the card. A successful answer is a
    /// settled payment.
    /// </summary>
    public async Task<Responses.RegularPayment> RegularPaymentAsync(Requests.RegularPayment payment, CancellationToken cancellationToken = default)
    {
        return new Responses.RegularPayment(await SendAsync(payment, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Give money back out of a payment the provider has settled, whole or in
    /// part. A refund that names no amount gives back everything the payment
    /// has left in it.
    /// </summary>
    public async Task<GiveBack> RefundPaymentAsync(RefundPayment refund, CancellationToken cancellationToken = default)
    {
        return new GiveBack(await SendAsync(refund, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Take back the whole of a payment the provider has not settled yet.
    /// Anything less than the whole of it goes back as a refund instead.
    /// </summary>
    public async Task<GiveBack> CancelPaymentAsync(CancelPayment cancel, CancellationToken cancellationToken = default)
    {
        return new GiveBack(await SendAsync(cancel, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Payments as they stand — by token, every attempt under one of the
    /// merchant's own references, or the ones made between two days; the
    /// refused ones included, oldest first.
    /// </summary>
    public async Task<PaymentList> RetrievePaymentsAsync(RetrievePayments payments, CancellationToken cancellationToken = default)
    {
        return new PaymentList(await SendAsync(payments, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Ask what is known about a card from the head of its number, and how an
    /// amount may be paid off on it. Nothing is charged and nothing is written
    /// down.
    /// </summary>
    public async Task<Bin> RetrieveBinAsync(RetrieveBin retrieveBin, CancellationToken cancellationToken = default)
    {
        return new Bin(await SendAsync(retrieveBin, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Open an order to be paid on the gateway's own page, or overwrite the
    /// open one already under the same reference. Nothing is charged here; the
    /// customer is sent to the address that comes back and pays there.
    /// </summary>
    public async Task<OrderDetails> CreateOrderAsync(CreateOrder order, CancellationToken cancellationToken = default)
    {
        return new OrderDetails(await SendAsync(order, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Orders as they stand, each with its customer.
    /// </summary>
    public async Task<OrderList> RetrieveOrdersAsync(RetrieveOrders orders, CancellationToken cancellationToken = default)
    {
        return new OrderList(await SendAsync(orders, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Change an open order. Only what is sent is written.
    /// </summary>
    public async Task<OrderDetails> UpdateOrderAsync(UpdateOrder order, CancellationToken cancellationToken = default)
    {
        return new OrderDetails(await SendAsync(order, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Open a payment link, or overwrite the one already under the same
    /// reference. The address that comes back is the link itself.
    /// </summary>
    public async Task<PaymentLinkDetails> CreatePaymentLinkAsync(CreatePaymentLink link, CancellationToken cancellationToken = default)
    {
        return new PaymentLinkDetails(await SendAsync(link, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Payment links as they stand, each with the latest fifty payment attempts
    /// made on it and how many there have been in all.
    /// </summary>
    public async Task<PaymentLinkList> RetrievePaymentLinksAsync(RetrievePaymentLinks links, CancellationToken cancellationToken = default)
    {
        return new PaymentLinkList(await SendAsync(links, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Change a payment link: its lines, its last day, whether it takes
    /// payments. Only what is sent is written.
    /// </summary>
    public async Task<PaymentLinkDetails> UpdatePaymentLinkAsync(UpdatePaymentLink link, CancellationToken cancellationToken = default)
    {
        return new PaymentLinkDetails(await SendAsync(link, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Open a subscription, its first renewal to be paid on the gateway's own
    /// page and the rest taken from the card kept then; or overwrite the one
    /// already under the same reference while nothing has been paid on it.
    /// </summary>
    public async Task<SubscriptionDetails> CreateSubscriptionAsync(CreateSubscription subscription, CancellationToken cancellationToken = default)
    {
        return new SubscriptionDetails(await SendAsync(subscription, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Subscriptions as they stand, each with its customer and the renewal it
    /// is on.
    /// </summary>
    public async Task<SubscriptionList> RetrieveSubscriptionsAsync(RetrieveSubscriptions subscriptions, CancellationToken cancellationToken = default)
    {
        return new SubscriptionList(await SendAsync(subscriptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Change a subscription, or call it off with the status <c>cancelled</c>.
    /// Only what is sent is written. Nothing is given back on a cancellation:
    /// the customer is served to the end of what they paid for, and nothing
    /// is charged after that.
    /// </summary>
    public async Task<SubscriptionDetails> UpdateSubscriptionAsync(UpdateSubscription subscription, CancellationToken cancellationToken = default)
    {
        return new SubscriptionDetails(await SendAsync(subscription, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Keep a card for a customer without making a payment on it.
    /// </summary>
    public async Task<SavedCardDetails> CreateSavedCardAsync(CreateSavedCard savedCard, CancellationToken cancellationToken = default)
    {
        return new SavedCardDetails(await SendAsync(savedCard, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Kept cards — by token, every card of a customer by their reference, or
    /// the ones kept between two days — each with its customer, the default
    /// first.
    /// </summary>
    public async Task<SavedCardList> RetrieveSavedCardsAsync(RetrieveSavedCards savedCards, CancellationToken cancellationToken = default)
    {
        return new SavedCardList(await SendAsync(savedCards, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Make one of a customer's kept cards the one they pay with unless they
    /// say otherwise.
    /// </summary>
    public async Task<SavedCardDetails> UpdateSavedCardAsync(UpdateSavedCard savedCard, CancellationToken cancellationToken = default)
    {
        return new SavedCardDetails(await SendAsync(savedCard, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Let go of a kept card, at the provider and here.
    /// </summary>
    public async Task<DeletedSavedCard> DeleteSavedCardAsync(DeleteSavedCard savedCard, CancellationToken cancellationToken = default)
    {
        return new DeletedSavedCard(await SendAsync(savedCard, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Read a word the gateway posted to one of the merchant's webhook
    /// addresses. Hand it the request exactly as it arrived — the method, the
    /// path of the address it came to (without the query string), the raw body
    /// byte for byte and the two headers — and nothing in it is believed until
    /// the signature is checked against the secret.
    /// </summary>
    /// <remarks>
    /// The word only names what it is about; ask the gateway what became of it
    /// before acting on it. Answer with any 2xx once the word is taken; the
    /// gateway tries again, up to five times, until it hears one.
    /// </remarks>
    /// <exception cref="SignatureException">When the signature does not hold.</exception>
    public Responses.Webhook Webhook(string method, string path, byte[] payload, string? timestamp, string? signature)
    {
        if (!VerifyWebhook(method, path, payload, timestamp, signature))
        {
            throw new SignatureException("Bildirimin imzası doğrulanamadı; bildirim ödeme geçidinden gelmemiş olabilir.");
        }

        return new Responses.Webhook(Decode(payload, 0));
    }

    public Responses.Webhook Webhook(string method, string path, string payload, string? timestamp, string? signature)
    {
        return Webhook(method, path, Encoding.UTF8.GetBytes(payload), timestamp, signature);
    }

    /// <summary>
    /// Whether a word that arrived at a webhook address was signed by the
    /// gateway with this team's secret, recently enough to be taken. The path
    /// is the address's own, with its leading slash and without the query
    /// string; the body is the raw bytes as they arrived.
    /// </summary>
    public bool VerifyWebhook(string method, string path, byte[] payload, string? timestamp, string? signature)
    {
        return _signature.Verify(method, path, payload, timestamp, signature);
    }

    public bool VerifyWebhook(string method, string path, string payload, string? timestamp, string? signature)
    {
        return VerifyWebhook(method, path, Encoding.UTF8.GetBytes(payload), timestamp, signature);
    }

    /// <summary>
    /// Sign what is being asked for, hand it to the gateway and read the answer
    /// back. The body is signed exactly as it is sent, byte for byte, so it is
    /// written once and used for both.
    /// </summary>
    private async Task<JsonElement> SendAsync(Message message, CancellationToken cancellationToken)
    {
        var method = message.Method;
        var path = _options.Path(message.Path);
        var body = Encoding.UTF8.GetBytes(message.ToBody().ToJsonString(Fields.Json));
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        using var request = new HttpRequestMessage(new HttpMethod(method), _options.Url(message.Path));

        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add(Options.ApiKeyHeader, _options.ApiKey);
        request.Headers.Add(Signature.TimestampHeader, timestamp.ToString(CultureInfo.InvariantCulture));
        request.Headers.Add(Signature.Header, _signature.Sign(method, path, body, timestamp));

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.Timeout);

        int status;
        byte[] payload;
        string? signature;
        string? signedAt;
        string? retryAfter;

        try
        {
            using var response = await _http.SendAsync(request, timeout.Token).ConfigureAwait(false);
            status = (int)response.StatusCode;
            payload = await response.Content.ReadAsByteArrayAsync(timeout.Token).ConfigureAwait(false);
            signature = Header(response, Signature.Header);
            signedAt = Header(response, Signature.TimestampHeader);
            retryAfter = Header(response, "Retry-After");
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TransportException("Ödeme geçidine ulaşılamadı: yanıt süresinde gelmedi.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new TransportException("Ödeme geçidine ulaşılamadı: " + exception.Message, exception);
        }

        return ReadAnswer(status, payload, method, path, signedAt, signature, retryAfter);
    }

    /// <summary>
    /// A header of the answer, or nothing when it did not carry it.
    /// </summary>
    private static string? Header(HttpResponseMessage response, string name)
    {
        var value = response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>
    /// Read the answer. An outcome is answered with 200 and signed, however the
    /// payment itself turned out: a payment the provider declined is an
    /// outcome like any other and comes back rather than being thrown. The
    /// signature is checked over the method and the path of the request and
    /// the answer's own moment and body.
    /// </summary>
    /// <remarks>
    /// Anything else is a refusal — the request never became a payment — and
    /// the status says which kind: 401 credentials, 403 what the team may not
    /// do, 404 a record that is not the team's, 422 the fields, 429 too many
    /// requests in a minute. The gateway signs some of those too, but a
    /// signature does not make a refusal an outcome, so the status is read
    /// first.
    /// </remarks>
    private JsonElement ReadAnswer(int status, byte[] payload, string method, string path, string? signedAt, string? signature, string? retryAfter)
    {
        if (status == 200)
        {
            if (!_signature.Verify(method, path, payload, signedAt, signature))
            {
                throw new SignatureException("Yanıtın imzası doğrulanamadı; yanıt ödeme geçidinden gelmemiş olabilir.");
            }

            return Decode(payload, status);
        }

        var body = Parse(payload) ?? default;
        var result = body.Field("result");
        var message = RefusalMessage(body, result);

        throw status switch
        {
            401 => new AuthenticationException(message),
            403 => new ForbiddenException(message),
            404 => new NotFoundException(message),
            422 => new ValidationException(message, RefusalErrors(body, result)),
            429 => new RateLimitException(message, int.TryParse(retryAfter, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ? seconds : null),
            _ => new UnexpectedResponseException(message, status),
        };
    }

    /// <summary>
    /// What a refusal says. The gateway answers in the one shape it answers
    /// everything in, so what went wrong is found under <c>result</c>.
    /// </summary>
    private static string RefusalMessage(JsonElement body, JsonElement result)
    {
        if (result.Field("message").ValueKind == JsonValueKind.String)
        {
            return result.Field("message").GetString()!;
        }

        if (body.Field("message").ValueKind == JsonValueKind.String)
        {
            return body.Field("message").GetString()!;
        }

        return "Ödeme geçidi isteği reddetti.";
    }

    /// <summary>
    /// Which fields a refusal is about, each with the reasons it was refused.
    /// </summary>
    private static IReadOnlyDictionary<string, IReadOnlyList<string>> RefusalErrors(JsonElement body, JsonElement result)
    {
        var errors = result.Field("errors").ValueKind == JsonValueKind.Object ? result.Field("errors") : body.Field("errors");
        var fields = new Dictionary<string, IReadOnlyList<string>>();

        if (errors.ValueKind == JsonValueKind.Object)
        {
            foreach (var field in errors.EnumerateObject())
            {
                fields[field.Name] = field.Value.ValueKind == JsonValueKind.Array
                    ? field.Value.EnumerateArray().Select(reason => reason.ToString()).ToArray()
                    : new[] { field.Value.ToString() };
            }
        }

        return fields;
    }

    /// <summary>
    /// Read a body the signature has already vouched for.
    /// </summary>
    private static JsonElement Decode(byte[] payload, int status)
    {
        return Parse(payload) ?? throw new UnexpectedResponseException($"Ödeme geçidi {status} durumuyla okunamayan bir yanıt döndü.", status);
    }

    private static JsonElement? Parse(byte[] payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);

            return document.RootElement.ValueKind == JsonValueKind.Object ? document.RootElement.Clone() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
