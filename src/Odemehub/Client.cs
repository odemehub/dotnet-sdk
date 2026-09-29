using System;
using System.Collections.Generic;
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
/// A client holds no state beyond its options, so one can be shared across
/// the application for its whole life, e.g. registered as a singleton.
/// </remarks>
public sealed class Client
{
    private static readonly HttpClient SharedHttp = new() { Timeout = System.Threading.Timeout.InfiniteTimeSpan };

    private readonly Options _options;
    private readonly HttpClient _http;
    private readonly Signature _signature;

    /// <param name="options">The address, the credentials and the channel.</param>
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
    /// the address it comes back with.
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
    /// Open an order to be paid on the gateway's own page, and get back the
    /// address to send the customer to.
    /// </summary>
    public async Task<Responses.OrderPayment> OrderPaymentAsync(Requests.OrderPayment orderPayment, CancellationToken cancellationToken = default)
    {
        return new Responses.OrderPayment(await SendAsync(orderPayment, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Open a subscription. The customer is sent to the address it comes back
    /// with and pays there, and the periods after that are taken from the
    /// card they pay with.
    /// </summary>
    public async Task<Subscription> SubscriptionPaymentAsync(SubscriptionPayment subscription, CancellationToken cancellationToken = default)
    {
        return new Subscription(await SendAsync(subscription, cancellationToken).ConfigureAwait(false));
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
    /// How a payment went. A customer sent to their bank comes back to the
    /// merchant with the payment's token and a hint at how it went; the hint
    /// is worth nothing on its own, and this call says what really became of it.
    /// </summary>
    public async Task<Responses.Payment> RetrievePaymentAsync(RetrievePayment payment, CancellationToken cancellationToken = default)
    {
        return new Responses.Payment(await SendAsync(payment, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Ask what the gateway's provider knows about a card by the head of its
    /// number, and how an amount may be paid off on it. Nothing is charged and
    /// nothing is written down.
    /// </summary>
    public async Task<Bin> RetrieveBinAsync(RetrieveBin retrieveBin, CancellationToken cancellationToken = default)
    {
        return new Bin(await SendAsync(retrieveBin, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Save a product in the merchant's catalogue at the gateway, or change the
    /// one already saved under the same key on the same channel. Order lines
    /// and subscriptions name products by key.
    /// </summary>
    public async Task<Product> SaveProductAsync(SaveProduct product, CancellationToken cancellationToken = default)
    {
        return new Product(await SendAsync(product, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Where a subscription stands: what it is for, the period it is on and
    /// whether that period has been paid for.
    /// </summary>
    public async Task<Subscription> RetrieveSubscriptionAsync(RetrieveSubscription subscription, CancellationToken cancellationToken = default)
    {
        return new Subscription(await SendAsync(subscription, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Call a subscription off. Nothing is given back: the customer keeps the
    /// days they already paid for and is served to the end of them, and
    /// nothing is charged after that.
    /// </summary>
    public async Task<Subscription> CancelSubscriptionAsync(CancelSubscription subscription, CancellationToken cancellationToken = default)
    {
        return new Subscription(await SendAsync(subscription, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Keep a card for a customer without making a payment on it.
    /// </summary>
    public async Task<KeptCard> SaveCardAsync(SaveCard saveCard, CancellationToken cancellationToken = default)
    {
        return new KeptCard(await SendAsync(saveCard, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// The cards a customer let the merchant keep, the default one first.
    /// </summary>
    public async Task<KeptCards> SavedCardsAsync(SavedCards savedCards, CancellationToken cancellationToken = default)
    {
        return new KeptCards(await SendAsync(savedCards, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Make one of a customer's kept cards the one they pay with unless they
    /// say otherwise.
    /// </summary>
    public async Task<KeptCard> DefaultSavedCardAsync(DefaultSavedCard defaultSavedCard, CancellationToken cancellationToken = default)
    {
        return new KeptCard(await SendAsync(defaultSavedCard, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Let go of one of a customer's kept cards, at the provider and here.
    /// </summary>
    public async Task<KeptCard> DeleteSavedCardAsync(DeleteSavedCard deleteSavedCard, CancellationToken cancellationToken = default)
    {
        return new KeptCard(await SendAsync(deleteSavedCard, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Read the word the gateway sent about a subscription: posted to the
    /// address the subscription was opened with, as plain JSON signed in the
    /// <c>X-Signature</c> header. Hand it the body exactly as it arrived, byte
    /// for byte, together with the header; nothing in it is to be believed
    /// until the signature holds.
    /// </summary>
    /// <exception cref="SignatureException">When the signature does not hold.</exception>
    public Responses.SubscriptionWebhook SubscriptionWebhook(byte[] payload, string? signature)
    {
        if (!_signature.Verify(payload, signature))
        {
            throw new SignatureException("Bildirimin imzası doğrulanamadı; bildirim ödeme geçidinden gelmemiş olabilir.");
        }

        return new Responses.SubscriptionWebhook(Decode(payload, 0));
    }

    public Responses.SubscriptionWebhook SubscriptionWebhook(string payload, string? signature)
    {
        return SubscriptionWebhook(Encoding.UTF8.GetBytes(payload), signature);
    }

    /// <summary>
    /// Sign what is being asked for, hand it to the gateway and read the answer
    /// back. The body is signed exactly as it is sent, byte for byte, so it is
    /// written once and used for both.
    /// </summary>
    private async Task<JsonElement> SendAsync(Message message, CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(message.ToBody(_options.ChannelToken).ToJsonString(Fields.Json));

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Url(message.Path))
        {
            Content = new ByteArrayContent(body),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add(Options.ApiKeyHeader, _options.ApiKey);
        request.Headers.Add(Signature.Header, _signature.Sign(body));

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.Timeout);

        int status;
        byte[] payload;
        string? signature;

        try
        {
            using var response = await _http.SendAsync(request, timeout.Token).ConfigureAwait(false);
            status = (int)response.StatusCode;
            payload = await response.Content.ReadAsByteArrayAsync(timeout.Token).ConfigureAwait(false);
            signature = response.Headers.TryGetValues(Signature.Header, out var values) ? values.FirstOrDefault() : null;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TransportException("Ödeme geçidine ulaşılamadı: yanıt süresinde gelmedi.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new TransportException("Ödeme geçidine ulaşılamadı: " + exception.Message, exception);
        }

        return ReadAnswer(status, payload, string.IsNullOrEmpty(signature) ? null : signature);
    }

    /// <summary>
    /// Read the answer. An outcome is answered with 200 and signed, however the
    /// payment itself turned out: a payment the provider declined is an
    /// outcome like any other and comes back rather than being thrown.
    /// </summary>
    /// <remarks>
    /// Anything else is a refusal — the request never became a payment — and
    /// the status says which kind. The gateway signs some of those too, but a
    /// signature does not make a refusal an outcome, so the status is read
    /// first.
    /// </remarks>
    private JsonElement ReadAnswer(int status, byte[] payload, string? signature)
    {
        if (status == 200)
        {
            if (!_signature.Verify(payload, signature))
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
            422 => new ValidationException(message, RefusalErrors(body, result)),
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
