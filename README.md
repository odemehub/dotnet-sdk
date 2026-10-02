# ödemehub .NET SDK

ödemehub ödeme geçidini kendi uygulamanızdan kullanmak için hazırlanmış .NET istemcisi. Kart çekmek, 3D ödeme başlatmak, sipariş, abonelik ve ödeme linki açmak, kart saklamak, iade ve iptal yapmak, taksit sormak: hepsi burada.

İstemci her isteği gizli anahtarınızla imzalar, gelen her yanıtın imzasını doğrular. Siz imza, başlık ya da JSON ayrıntılarıyla uğraşmazsınız. Her uç nokta için bir metot vardır ve adı uç noktanın adıdır: `create-order` için `CreateOrderAsync()`, `retrieve-saved-cards-by-reference` için `RetrieveSavedCardsByReferenceAsync()`. Yalnızca .NET'in kendi `HttpClient`'ını ve `System.Text.Json`'ını kullanır; başka paket gerekmez.

## Kurulum

.NET 8 ve üzeri gerekir.

```bash
dotnet add package Odemehub.Sdk
```

## Yapılandırma

Dört bilgi gerekir. Hepsi paneldeki **Entegrasyon** sayfasındadır (menünün en altında): Çalışma Alanı Kimliğiniz, API anahtarı, gizli anahtar ve kanalınızın token'ı.

```csharp
using Odemehub;

var client = new Client(new Options
{
    BaseUrl = "https://app.odemehub.com",
    Team = "1000000001",                                   // Çalışma Alanı Kimliği
    ChannelToken = "6f1c2e7a-4b3d-4c8e-9a61-2f5d7b0c3e14", // müşterinin size ulaştığı kanal
    ApiKey = Environment.GetEnvironmentVariable("ODEMEHUB_API_KEY")!,
    ApiSecret = Environment.GetEnvironmentVariable("ODEMEHUB_API_SECRET")!,
});
```

Gizli anahtar hiçbir zaman tel üzerinden gitmez; yalnızca imza üretmekte ve doğrulamakta kullanılır. Anahtarları kodun içine yazmayın; ortam değişkeninde ya da gizli anahtar deposunda tutun.

Kanal token'ı entegrasyon için bir kez verilir ve her isteğe istemci yazar. Birden çok kanalda satıyorsanız tek bir istekte `ChannelToken` vererek o isteği başka kanala yazdırabilirsiniz. Geçit hiçbir yerde veritabanı numarası kullanmaz: kanal, ödeme hesabı, işlem, sipariş, abonelik, link ve kayıtlı kart her zaman token'ıyla anılır.

İstemci durum tutmaz; uygulama boyunca tek bir nesneyi paylaşın (ASP.NET Core'da `builder.Services.AddSingleton(client)`). İstek bir dakika içinde yanıt almazsa kesilir; süreyi `Options.Timeout` ile değiştirebilirsiniz. Bütün metotlar `CancellationToken` alır. `IHttpClientFactory`'den aldığınız bir `HttpClient`'ı `new Client(options, httpClient)` ile verebilirsiniz.

İstekler `Odemehub.Requests` ad alanındaki nesnelerdir ve nesne başlatıcıyla kurulur. Zorunlu alanlar `required` işaretlidir; eksik bırakırsanız kod derlenmez. İsteğe bağlı bir alanı vermezseniz gövdeye hiç yazılmaz. SDK alanları kendisi denetlemez; kuralları geçit uygular ve hatayı alan alan `ValidationException` ile döner. Kart numarası ve güvenlik kodu `ToString()` çıktısında `*****` görünür; kart nesnesi yanlışlıkla loglansa da kart bilgisi görünmez.

Yanıtlar `Odemehub.Responses` ad alanındadır ve geçidin JSON'unu birebir yansıtır: `payment.Transaction.Status`, `order.Order.Customer` gibi. Para birimi, dönem ve durumlar `Odemehub.Enums` altındaki enum'lardır (`Currency`, `Period`, `OrderStatus`, `SubscriptionStatus`, `TransactionStatus`, `PaymentStatus`, `SecurityType`, `RefundType`, `RefundStatus`, `CardScheme`, `CardType`). Geçit bu sürümün tanımadığı yeni bir değer gönderirse yanıt yine okunur ve değer `Unknown` olur. `Odemehub.Requests` ile `Odemehub.Enums`'u `using` ile ekleyip yanıtları `var` ile karşılamak en rahatıdır.

## İmza

Her istek üç başlıkla gider: `X-Api-Key`, `X-Timestamp` (Unix saniye) ve `X-Signature`. İmza, `"{timestamp}\n{METHOD}\n{path}\n{body}"` metni üzerinden gizli anahtarla alınan HMAC-SHA256'nın küçük harfli hex hâlidir. `path` adresin sorgu dizesiz yolu (`/api/1000000001/gateway/regular-payment`), `body` gönderilen JSON'ın kendisidir; GET isteklerinde boş dizedir. Zaman damgası sunucu saatinden 5 dakikadan uzak olamaz; sunucunuzun saati kaymışsa istekler `AuthenticationException` ile döner. Geçit her yanıtı aynı yöntemle imzalar; istemci yanıtı isteğin metodu ve yoluyla, yanıtın kendi `X-Timestamp` değeriyle doğrular. Test vektörü en alttadır.

## Karttan doğrudan çekim

Müşteri hiçbir yere gitmez. Başarılı yanıt, paranın alındığı anlamına gelir.

```csharp
using Odemehub.Enums;
using Odemehub.Requests;

var customer = new Customer
{
    Reference = "musteri-88",                 // sizdeki müşteri anahtarı; kart saklanacaksa zorunlu
    BillingAddress = new Address
    {
        Firstname = "Ahmet",
        Lastname = "Yılmaz",
        Email = "ahmet@ornek.com",
        Phone = "05551112233",
        AddressLine = "Kızılırmak Mah. Dumlupınar Blv. No:3",   // gövdede "address"
        District = "Çankaya",
        Province = "Ankara",
        Country = "Türkiye",
    },
};

var card = new Card
{
    HolderName = "AHMET YILMAZ",
    Number = "5400 3600 0000 0003",           // boşluklu ya da boşluksuz
    ExpiryMonth = "12",
    ExpiryYear = "2030",
    SecurityCode = "000",
    ShouldSave = true,                        // isteğe bağlı: başarılı ödemeden sonra kartı sakla
};

var payment = await client.RegularPaymentAsync(new RegularPayment
{
    ChannelReference = "SIP-10231",           // sizdeki referans; en az bir rakam içermeli
    Amount = "450.00",
    InstallmentNumber = 1,
    Ip = httpContext.Connection.RemoteIpAddress!.ToString(),
    Customer = customer,
    Card = card,
});

if (payment.Result.IsSuccessful)
{
    // payment.Transaction.Token — iade ve iptalde ödeme bununla adlandırılır
    // payment.SavedCard?.Token   — ShouldSave verildiyse ve kart saklandıysa
}
```

Reddedilen ödeme de bir sonuçtur: `Result.IsSuccessful` false, `Result.Message` neden. Yalnızca geçit isteğin kendisini reddederse (hatalı alan, yetki, hız sınırı, bulunamayan kayıt) istisna fırlatılır.

Ödemede müşterinin fatura adresi (`BillingAddress`) sekiz alanıyla eksiksiz gönderilir; şirket adına alışverişte `CompanyTitle`, `TaxNumber` ve `TaxOffice` birlikte eklenir. Ödemeye gönderim adresi gönderilmez.

Kayıtlı kartla ödemede `Card` yerine `SavedCardToken` verilir; ödeme kartın saklandığı hesaptan geçer, `PaymentProviderToken` gönderilmez. Kart hangi kanal ve müşteri referansıyla saklandıysa ödeme de aynılarını taşımalıdır.

Tutarlar her zaman `string`'dir ve nokta ayraçlı, en çok iki ondalıklıdır: `"100"`, `"100.1"`, `"100.10"`; imzalanıp gönderildiği gibi kalır, yolda yuvarlanmaz. Tutar sıfırdan büyük ve en çok 10.000.000,00 olabilir. `Currency` (`Currency.TRY`, `USD`, `EUR`, `GBP`) boş bırakılırsa TRY'dir.

## 3D ödeme

Siz ödemeyi başlatırsınız, müşteri bankasına gider, banka müşteriyi sizin adresinize geri yollar.

```csharp
var payment = await client.SecurePaymentAsync(new SecurePayment
{
    ChannelReference = "SIP-10232",
    Amount = "450.00",
    InstallmentNumber = 1,
    Ip = ip,
    CallbackUrl = "https://magazam.com/odeme/donus",
    Customer = customer,
    Card = card,
});

if (payment.Result.IsSuccessful)
{
    return Results.Redirect(payment.RedirectUrl!);       // müşteriyi bankaya gönderin
}
```

Başarılı yanıt **ödeme alındı demek değildir**; yalnızca müşterinin gideceği adres hazır demektir. Müşteriyi **15 dakika içinde** bu adrese yönlendirin; sayfası o süre içinde açılmayan ödemenin süresi dolar (`expired`).

Banka işini bitirince müşterinin tarayıcısı `CallbackUrl` adresinize şu alanları POST eder: `transaction_token`, `channel_reference`, `successful` (`1` / `0`). Bu POST imzasızdır ve müşterinin tarayıcısından gelir; yalnızca ipucudur. Sonucu kendi imzalı bağlantınızdan sorun:

```csharp
app.MapPost("/odeme/donus", async ([FromForm(Name = "transaction_token")] string transactionToken, Client client) =>
{
    var outcome = await client.RetrievePaymentAsync(new RetrievePayment { Token = transactionToken });

    if (outcome.Result.IsSuccessful)
    {
        // siparişi ödendi olarak işaretleyin
    }

    outcome.Transaction.Status;          // TransactionStatus.Successful, Failed, Expired ...
    outcome.Transaction.PaymentStatus;   // paranın akıbeti (sonradan Refunded olabilir)
    outcome.Transaction.Amount;          // karttan çekilen tutar
    // ...
}).DisableAntiforgery();
```

`successful` alanına bakıp sipariş kapatmayın — onu herkes gönderebilir. Geçide sorduğunuz yanıt ise her zaman imzalıdır ve SDK imzayı sizin için doğrular. Başkasının ya da hiç olmayan bir işlemi sorarsanız `NotFoundException` alırsınız.

Geçidin kendi ödeme yanıtları (`SecurePaymentAsync`, `RegularPaymentAsync`, `RefundPaymentAsync`, `CancelPaymentAsync`, `RetrievePaymentAsync`, `RetrievePaymentByReferenceAsync`) aynı şekli döner: `Result`, `Transaction` (`Token`, `ChannelToken`, `ChannelReference`, `Status`, `PaymentStatus`, `SecurityType`, `Amount`, `BaseAmount`, `Currency`, `InstallmentNumber`, `IsTest`, `CreatedAt`, ödeme bir siparişte, linkte ya da abonelikte alındıysa `OrderToken` / `PaymentLinkToken` / `SubscriptionToken`), `Customer` (`Reference` ve `BillingAddress`), `Conversion`, kart saklandıysa `SavedCard`; iade ve iptalde ayrıca `Refund`, 3D'de `RedirectUrl`.

Müşteri bankadan sonra sekmeyi kapatırsa tarayıcı `CallbackUrl` adresinize hiç dönmez; bunun için panelde kanala `transaction.*` webhook'u tanımlayın (bkz. [Webhook](#webhook)). Bildirim hiç gelmezse `RetrievePaymentByReferenceAsync()` ile sorabilirsiniz.

## Sipariş

Kart sizde sorulmaz. Siparişi açarsınız, geçit kendi ödeme sayfasının adresini döner, müşteri orada öder. Tutar gönderilmez: geçit kalemleri ve seçilen gönderim yöntemini toplar. Birim tutarlar KDV dahildir (120 ve %20 → 100 mal + 20 KDV).

```csharp
var created = await client.CreateOrderAsync(new CreateOrder
{
    ChannelReference = "SIP-10233",
    SuccessUrl = "https://magazam.com/odeme/donus",
    Items =
    [
        new Item { Name = "Kulaklık", UnitAmount = "1200.00", Quantity = 1, TaxRate = "20", ChannelReference = "SKU-1" },
        new Item { Name = "Hediye paketi", UnitAmount = "25.00", Quantity = 1, TaxRate = "20", Image = "https://magazam.com/img/paket.jpg" },
    ],
    Customer = customer,                                   // bilinen kadarı; kalanı sayfada sorulur
    ShippingMethods =
    [
        new ShippingMethod { Handle = "standart", Title = "Standart Kargo", Amount = "49.90", TaxRate = "20" },
    ],
    RequiresShippingAddress = true,
    CancelUrl = "https://magazam.com/sepet",
});

return Results.Redirect(created.Order.CheckoutUrl!);       // müşteriyi buraya gönderin
```

Siparişte müşterinin her parçası isteğe bağlıdır: `Reference`, `BillingAddress`, `ShippingAddress` ya da hiçbiri. Verilenler ödeme sayfasında dolu gelir, kalanı ödeyene sorulur. Referans verilmeyen siparişe geçit ödeme anında `guest-…` referansı yazar (`Customer.IsGuest`).

Yanıttaki `Order` siparişi bütünüyle taşır: `Token`, `ChannelToken`, `ChannelReference`, `Description`, `PaymentProviderToken`, `Status` (`OrderStatus.Open` / `Paid`), `Items`, `ShippingMethods`, `ShippingMethod` (ödeyenin seçtiği), `Subtotal`, `ShippingAmount`, `TaxAmount`, `Amount`, `Currency`, `IsTest`, `CreatedAt`, `CheckoutUrl` (ödenince `null`), `Transaction` (ödeyen işlem; açıkken `null`) ve `Customer`. Müşteri yanıtın üst seviyesinde de durur: `created.Customer` ile `created.Order.Customer` aynıdır; listelerde her siparişin kendi `Customer`'ı vardır.

Ödendiğinde müşteri `SuccessUrl` adresinize 3D dönüşüyle aynı alanlarla POST edilir; kesin sonucu `RetrieveOrderAsync()` verir; `order.paid` webhook'u geldiğinde de onu çağırın. `Order.Transaction.PaymentStatus` sonradan yapılan iadeyi gösterir.

```csharp
var current = await client.RetrieveOrderAsync(new RetrieveOrder { Token = token });

if (current.Order.IsPaid)
{
    // current.Order.Transaction?.Token ile iade / iptal / RetrievePaymentAsync yapılabilir
}

// Açık siparişte yalnızca gönderilen alanlar değişir; kalemler gönderilirse tamamı yenilenir.
await client.UpdateOrderAsync(new UpdateOrder { Token = token, Description = "Hediye paketi", Clear = ["cancel_url"] });
```

`Clear`, bir alanı boşaltmak içindir (alanı göndermemek eskisini korur). `ShippingMethods = []` bütün gönderim yöntemlerini kaldırır.

`Create*` çağrıları aynı kanal ve referans için tekrarlanabilir: aynı referansla ikinci kez açılan sipariş, link ya da (henüz ödenmemiş) abonelik yeni gönderilenlerle güncellenir ve kendi token'ıyla döner. Ödenmiş sipariş değişmez. **Bekleyen ödeme varken güncellenemez:** ödeme sayfasında son 15 dakika içinde başlamış bir ödeme varsa `Create*` ve `Update*` çağrıları `channel_reference` / `token` alanında reddedilir.

## Ödeme linki

Link, adresi bilen herkesin ödeyebileceği bir sayfadır; kapatılana ya da son gününe kadar tekrar tekrar ödenir. Müşterisi yoktur; ödeyen kim olduğunu sayfada söyler.

```csharp
var link = await client.CreatePaymentLinkAsync(new CreatePaymentLink
{
    Items = [new Item { Name = "Bağış", UnitAmount = "100.00", Quantity = 1, TaxRate = "0" }],
    Currency = Currency.TRY,
    ChannelReference = "LNK-1",          // boş bırakılırsa geçit LINK{n} üretir
    ExpiresAt = "2026-12-31",            // çalışma alanının saat dilimine göre son gün
});

link.PaymentLink.CheckoutUrl;            // linkin kendisi; ödenemezken (kapalı, süresi geçmiş) null
link.PaymentLink.ExpiresAt;              // verilen günün sonu (çalışma alanı saatiyle), ISO 8601 UTC
link.PaymentLink.IsActive;               // açık ve süresi geçmemiş mi
link.PaymentLink.IsTest;                 // ödemeleri şu an test ortamında mı alınıyor

var detail = await client.RetrievePaymentLinkAsync(new RetrievePaymentLink { Token = link.PaymentLink.Token });
detail.Transactions;                     // son 50 deneme, yeniden eskiye
detail.TransactionsCount;                // linkteki denemelerin tamamının sayısı
detail.Successful;                       // listelenenlerden başarılı olanlar

await client.UpdatePaymentLinkAsync(new UpdatePaymentLink { Token = link.PaymentLink.Token, IsActive = false });
```

Kanal verilmezse link istemcinin kanalına açılır. Panelin açtığı linklere ulaşmak (ya da linki oraya açmak) için kanal olarak `ChannelToken = ChannelMessage.OdemehubChannel` verin; yanıtta bu linklerin `ChannelToken`'ı `null`dır. Süresi geçmiş bir linki yeniden açmak için `IsActive = true` ile birlikte yeni bir `ExpiresAt` gönderin; son günü kaldırmak için `Clear = ["expires_at"]`.

## Abonelik

İlk yenileme ödeme sayfasında ödenir ve kart orada saklanır; sonrakiler o karttan çekilir. Müşteri referansı zorunludur, çünkü kart onun altında saklanır. Ödeme hesabı (verilen ya da varsayılan) kart saklamalı ve 3D ödeme almalı, planınız kayıtlı kartları kapsamalıdır.

```csharp
var opened = await client.CreateSubscriptionAsync(new CreateSubscription
{
    ChannelReference = "ABO-1",
    Period = Period.Monthly,             // Daily | Weekly | Monthly | Annually
    SuccessUrl = "https://magazam.com/abonelik/donus",
    Items = [new Item { Name = "Premium", UnitAmount = "99.90", Quantity = 1, TaxRate = "20" }],
    Customer = customer,
    RenewalLimit = 12,                   // boş: iptale kadar
});

return Results.Redirect(opened.Subscription.CheckoutUrl!);
```

```csharp
var current = await client.RetrieveSubscriptionAsync(new RetrieveSubscription { Token = token });
var subscription = current.Subscription;

subscription.Status;            // SubscriptionStatus.Pending | Active | PastDue | Cancelled | Completed
subscription.Renewal.Amount;    // içinde bulunulan yenilemenin tutarı
subscription.Renewal.PaidAt;    // ödendiyse ne zaman
subscription.Renewal.EndsAt;    // ödenen dönemin sonu
subscription.NextPaymentAt;     // sonraki tahsilat zamanı
subscription.RenewalsPaid;      // şimdiye kadar ödenen yenileme sayısı
subscription.CheckoutUrl;       // ödenmemiş yenileme varsa müşteriye verilecek adres
subscription.Customer?.Reference;   // current.Customer ile aynı

// Dönem, kalemler, ödeme sayısı değişir; iptal de buradan:
await client.UpdateSubscriptionAsync(new UpdateSubscription { Token = token, Status = SubscriptionStatus.Cancelled });
```

Kalemler gönderilirse ödenmemiş yenilemeye ve sonrakilere yansır. İptalde para iade edilmez; ödenmiş dönem sonuna kadar sürer, sonra abonelik biter. Ödenmiş dönem yoksa iptal hemen geçerlidir. `RenewalLimit` şimdiye kadar ödenen yenilemelerin altına inemez; sınırı kaldırmak için `Clear = ["renewal_limit"]`.

İlk ödemeden sonra kanal, ödeme hesabı, para birimi, dönem ve müşteri referansı değiştirilemez; geçit bunları 422 ile reddeder (aynı değeri yeniden göndermek değişiklik sayılmaz).

Dönem bitince yeni yenileme açılır ve müşterinin varsayılan kartından çekilir. Banka kabul etmezse çekim birkaç kez yeniden denenir; hiçbiri olmazsa abonelik `past_due` olur ve `CheckoutUrl` müşterinin kendisinin ödeyeceği adresi taşır.

## Webhook

Sipariş ödendiğinde, link ödemesi alındığında, abonelik durum değiştirdiğinde, API ödemesi bittiğinde ve bir ödeme iade ya da iptal edildiğinde geçidin **kendi sunucusu** imzalı bir JSON POST gönderir. Müşteri sekmeyi kapatıp `CallbackUrl` / `SuccessUrl` adresinize hiç dönmese de bu bildirim gelir. Adresler kodda verilmez; panelde **Ayarlar → Webhook** sayfasında kanal, olay ve adres seçilerek tanımlanır.

| Kaynak | Olaylar |
| --- | --- |
| Sipariş | `order.paid`, `order.payment_refunded`, `order.payment_cancelled` |
| Ödeme linki | `payment_link.paid`, `payment_link.payment_refunded`, `payment_link.payment_cancelled` |
| Abonelik | `subscription.active`, `subscription.past_due`, `subscription.cancelled`, `subscription.ended`, `subscription.completed`, `subscription.payment_refunded`, `subscription.payment_cancelled` |
| API ödemesi | `transaction.successful`, `transaction.failed`, `transaction.expired`, `transaction.payment_refunded`, `transaction.payment_cancelled` |

Sipariş, link ya da abonelikte alınan ödeme için `transaction.*` gelmez; o kaynağın kendi olayı gelir. Olay adları `Odemehub.Enums.WebhookEvent` sabitlerindedir.

**Webhook nihai sonuç değildir.** Gövde yalnızca kaynağın token'ını (para hareketi varsa yanında ödemenin token'ını) taşır. Kararı, token ile geçide sorduğunuz yanıta göre verin ve yanıtı kendi kaydınızla (referans, tutar, durum) karşılaştırın. Gövdeyi **ham** (`byte[]`) okuyun; bir nesneye çevirip yeniden yazarsanız imza tutmaz.

```csharp
app.MapPost("/odemehub/webhook", async (HttpRequest request, Client client) =>
{
    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer);

    Odemehub.Responses.Webhook webhook;

    try
    {
        webhook = client.Webhook(
            request.Method,
            request.Path,
            buffer.ToArray(),
            request.Headers["X-Timestamp"].FirstOrDefault(),
            request.Headers["X-Signature"].FirstOrDefault());
    }
    catch (SignatureException)
    {
        return Results.Unauthorized();
    }

    // webhook.Id: aynı bildirim tekrar gelebilir; bununla ayıklayın

    if (webhook.OrderToken is not null)
    {
        var order = (await client.RetrieveOrderAsync(new RetrieveOrder { Token = webhook.OrderToken })).Order;
        // order.Status == OrderStatus.Paid, order.Transaction?.PaymentStatus == PaymentStatus.Refunded ...
    }
    else if (webhook.SubscriptionToken is not null)
    {
        var subscription = (await client.RetrieveSubscriptionAsync(new RetrieveSubscription { Token = webhook.SubscriptionToken })).Subscription;
    }
    else if (webhook.TransactionToken is not null)   // transaction.* ve payment_link.*
    {
        var transaction = (await client.RetrievePaymentAsync(new RetrievePayment { Token = webhook.TransactionToken })).Transaction;
        // transaction.PaymentLinkToken: linkte alınan ödemede linkin token'ı
    }

    return Results.NoContent();
});
```

Abonelik ve link ödemelerinin iade/iptal olaylarında `TransactionToken` da gelir; `RetrievePaymentAsync()` yanıtındaki `OrderToken` / `PaymentLinkToken` / `SubscriptionToken` ödemenin gerçekten o kaynağa ait olduğunu gösterir. Yalnızca doğrulamak için `client.VerifyWebhook(...)` `bool` döner. 2xx dışında bir yanıt (ya da yanıtsızlık) başarısız sayılır; geçit 60 sn, 5 dk, 15 dk ve 30 dk arayla toplam 5 kez dener ve yönlendirmeleri izlemez.

## Kayıtlı kartlar

Kart ödeme sırasında (`ShouldSave = true`) ya da ödemesiz saklanır. Kanal ve müşteri referansı ikilisinin altında durur; kart yanıtlarındaki `Customer` yalnızca `Reference` taşır.

```csharp
var saved = await client.CreateSavedCardAsync(new CreateSavedCard { Customer = customer, Card = card });
saved.SavedCard?.Token;                  // sağlayıcı saklamadıysa null, nedeni Result.Message

var cards = await client.RetrieveSavedCardsByReferenceAsync(new RetrieveSavedCardsByReference
{
    CustomerReference = "musteri-88",
});
cards.Default;                           // varsayılan kart, varsa

await client.RetrieveSavedCardAsync(new RetrieveSavedCard { Token = cardToken });
await client.UpdateSavedCardAsync(new UpdateSavedCard { Token = cardToken });   // varsayılan yap
await client.DeleteSavedCardAsync(new DeleteSavedCard { Token = cardToken });   // delete-saved-card/{token}
```

Ödemesiz saklamada müşterinin referansı ve fatura adresi eksiksiz gönderilir. Güvenlik kodu, kartı küçük bir çekim yapıp geri vererek saklayan sağlayıcılar için gerekir; diğerlerinde boş bırakılabilir ve hiçbir yerde saklanmaz. Kayıtlı kart yanıtlarında kartın yalnızca ilk haneleri ve son dördü vardır.

## İade ve iptal

Ödemenin token'ı yeter. İade, sağlayıcının kapattığı ödemeden kısmen ya da tamamen; iptal, henüz kapatılmamış ödemenin tamamı.

```csharp
var refund = await client.RefundPaymentAsync(new RefundPayment { Token = token, Amount = "50.00" });   // tutar boş: kalanın tamamı
refund.Refund?.Type;                     // RefundType.Refund
refund.Refund?.Amount;                   // gerçekten geri giden tutar
refund.Transaction.PaymentStatus;        // PaymentStatus.Refunded | PartiallyRefunded ...

await client.CancelPaymentAsync(new CancelPayment { Token = token });
```

Kur çevirisiyle çekilen ödemede iade tutarı çekilen para birimindedir.

## Kart sorgusu ve taksitler

Kartın ilk 6–8 hanesiyle bankası, tipi ve tutara göre taksit seçenekleri. Hiçbir şey çekilmez.

```csharp
var bin = await client.RetrieveBinAsync(new RetrieveBin { Bin = "54003600", Amount = "450.00" });

if (bin.Result.IsSuccessful)
{
    bin.IssuerName;     // Garanti Bankası
    bin.Program;        // Bonus
    bin.Scheme;         // CardScheme.Mastercard
    bin.Type;           // CardType.Credit
    bin.IsCommercial;

    foreach (var installment in bin.Installments)
    {
        // 3 taksitte ayda 157.87, toplam 473.60
        Console.WriteLine($"{installment.Number} x {installment.Amount} = {installment.Total}");
    }
}
```

Yanıtta sorulan haneler `bin.Number` alanındadır (C#'ta bir özellik, içinde bulunduğu sınıfla aynı adı taşıyamaz; adreslerdeki `AddressLine` de bu yüzdendir).

Sorgu başarısız dönebilir: kart tanınmıyor olabilir ya da hesabınızın sağlayıcısı taksit vermiyor olabilir. İki durumda da satışı durdurmayın, tek çekimle devam edin.

## Tutarlar ve taksit

İki tutar vardır ve karıştırılmamalıdır:

| Alan | Anlamı |
| --- | --- |
| `Amount` | **Karttan çekilecek** tutar. Vade farkı varsa içindedir. |
| `BaseAmount` | **Sattığınız** tutar, vade farkından önceki hâli. Gönderilmezse `Amount` ile aynı kabul edilir. |

Taksit yalnızca Türk Lirası ödemelerde yapılır. USD, EUR ya da GBP ödemede `InstallmentNumber` `1` olmalıdır ve `RetrieveBinAsync()` taksit listesini boş döner; kur çevirisiyle TRY'den başka bir para birimine çekilen ödeme için de aynısı geçerlidir.

Taksitli satışta `RetrieveBinAsync` size o taksidin toplamını verir; onu `Amount` olarak, sattığınız tutarı `BaseAmount` olarak gönderin:

```csharp
var payment = await client.RegularPaymentAsync(new RegularPayment
{
    ChannelReference = "SIP-10234",
    Amount = "473.60",        // 3 taksitin toplamı
    BaseAmount = "450.00",    // satılan tutar
    InstallmentNumber = 3,
    // ...
});
```

## Ödeme hangi hesaptan geçer

`PaymentProviderToken` verirseniz ödeme o hesaptan geçer; sipariş, link ve abonelik açarken de aynı alan vardır ve müşteri ödeme sayfasında o hesaptan öder. Vermezseniz hesabı çalışma alanınız seçer: panelde **Ödeme Ayarları → Gate (Yönlendirme)** altındaki kurallar sırayla denenir ve ödemenin karşıladığı ilk kural hesabı belirler. Hiçbir kural tutmazsa ödeme varsayılan hesaptan geçer.

- Kuralın hesabı ödemeyi alamıyorsa (ödeme türünü ya da para birimini desteklemiyorsa) o kural atlanır.
- Kayıtlı kartla ödeme her zaman kartın saklandığı hesaptan geçer.
- Taksitleri `RetrieveBinAsync()` ile gösteriyorsanız orada da hesap vermeyin: taksitler ödemenin gideceği hesaptan gelir ve çekilen tutar gösterdiğinizle aynı olur.

## Kur çevirisi

Panelde **Ödeme Ayarları → Kur Çevirici** altında bir kural tanımladıysanız, o para biriminde gelen ödeme karttan kuralın para biriminde çekilir. İsteğinizde hiçbir şey değişmez; yanıttaki `Conversion` karttan ne çekildiğini söyler:

```csharp
if (payment.Conversion is not null)
{
    payment.Conversion.Amount;   // 4985.56
    payment.Conversion.Currency; // TRY
    payment.Conversion.Rate;     // 49.855560
}
```

Çevrilmeyen ödemede `Conversion` `null` gelir. Güncel kur alınamıyorsa ödeme alınmaz; `422` ile `transaction.currency` alanında hata döner.

## Referansla ve tarihle listeleme

Her kaynak kendi referansıyla ya da bir tarih aralığıyla bulunur. Referansla sorguda aynı referansı taşıyanlardan en son açılanı döner. Aralık en çok 7 gündür, `YYYY-MM-DD` biçimindedir ve çalışma alanının saat dilimindedir; boş bırakılırsa son 7 gün.

```csharp
// Yanıtı alınamayan bir ödemenin akıbeti: referanstaki son ödeme
await client.RetrievePaymentByReferenceAsync(new RetrievePaymentByReference { ChannelReference = "SIP-10231" });

// Kanaldaki bütün denemeler, reddedilenler dahil, durumu ve tutarıyla
var list = await client.RetrievePaymentsByChannelReferenceAsync(new RetrievePaymentsByChannelReference
{
    CreatedFrom = "2026-09-26",
    CreatedTo = "2026-10-02",
});

foreach (var transaction in list.Payments)
{
    transaction.Status;          // TransactionStatus; Timeout: sağlayıcı yanıt vermedi
    transaction.PaymentStatus;   // PaymentStatus.Paid, Refunded, PartiallyRefunded ...
    transaction.OrderToken;      // bağlı olduğu sipariş / link / abonelik, varsa
}
```

Aynısı `RetrieveOrderByReferenceAsync` / `RetrieveOrdersByChannelReferenceAsync`, `RetrieveSubscriptionByReferenceAsync` / `RetrieveSubscriptionsByChannelReferenceAsync`, `RetrievePaymentLinkByReferenceAsync` / `RetrievePaymentLinksByChannelReferenceAsync` için de geçerlidir. Sipariş ve abonelik listelerinde her kaydın `Customer`'ı da gelir.

## Hatalar

Bütün hatalar `OdemehubException`'dan türer; tek bir `catch` hepsini yakalar.

| Hata | Durum | Anlamı |
| --- | --- | --- |
| `AuthenticationException` | 401 | API anahtarı yanlış, imza tutmuyor ya da zaman damgası aralık dışında |
| `ForbiddenException` | 403 | Çalışma alanı işlem yapamıyor (ödenmemiş bakiye, plan) ya da plan bu özelliği kapsamıyor |
| `NotFoundException` | 404 | Token ya da kanal + referansla istenen kayıt (ödeme, sipariş, link, abonelik, kayıtlı kart) yok |
| `ValidationException` | 422 | Alan hataları; `Errors` noktalı alan adıyla (`transaction.amount`, `order.items.0.name`) |
| `RateLimitException` | 429 | İstek sınırı; `RetryAfter` saniye |
| `SignatureException` | — | Yanıtın ya da bildirimin imzası doğrulanamadı; içeriğe güvenmeyin |
| `TransportException` | — | Geçide ulaşılamadı ya da yanıt süresinde gelmedi; ödemenin akıbetini `RetrievePaymentByReferenceAsync` ile sorun |
| `UnexpectedResponseException` | 500, diğer | Geçitte beklenmeyen hata ya da okunamayan yanıt; `Status` HTTP kodunu verir |

```csharp
try
{
    await client.RegularPaymentAsync(payment);
}
catch (ValidationException exception)
{
    // exception.Errors["transaction.amount"]
}
catch (OdemehubException exception)
{
    // exception.Message — Türkçe
}
```

Reddedilen ödeme, iade ya da kart saklama istisna değildir; `Result.IsSuccessful` false ve `Result.Message` dolu döner. Ağ hatasında ödemeyi körlemesine tekrarlamayın: `TransportException` "olmadı" demek değil, "bilmiyorum" demektir. Kendi `CancellationToken`'ınızla iptal ettiğiniz istek ise her zamanki gibi `OperationCanceledException` fırlatır.

## İstek sınırları

Sınırlar çalışma alanı başına ve dakikalıktır:

| Sınır | Kapsam |
| --- | --- |
| 300 istek / dk | bütün uç noktalar |
| 60 istek / dk | `secure-payment`, `regular-payment`, `refund-payment`, `cancel-payment`, `create-saved-card`, `delete-saved-card` (300'e ek olarak) |

Aşıldığında `RateLimitException` döner; `RetryAfter` kadar bekleyip aynı isteği yeniden gönderin.

## 2.0.0'daki kırıcı değişiklikler

2.0.0, geçidin bugünkü API'sine geçiştir; 1.x koduyla uyumlu değildir.

- **İmza:** `X-Timestamp` başlığı geldi; imza artık zaman damgası, metot, yol ve gövde üzerinden alınır (yukarıda).
- **Uçlar:** `OrderPaymentAsync`, `SubscriptionPaymentAsync`, `SaveProductAsync`, `CancelSubscriptionAsync`, `RetrieveTransactionsAsync`, `SaveCardAsync`, `SavedCardsAsync`, `DefaultSavedCardAsync` kalktı. Yerlerine `Create*` / `Retrieve*` / `Retrieve*ByReference` / `Retrieve*sByChannelReference` / `Update*` ailesi geldi (sipariş, ödeme linki, abonelik, kayıtlı kart). Abonelik iptali `UpdateSubscriptionAsync` ile `Status = SubscriptionStatus.Cancelled`'dır. Token'la sorgular GET'tir.
- **Token parametresi:** tekil kaydı adlandıran istek alanı her yerde `Token`'dır (`RefundPayment`, `CancelPayment`, `RetrievePayment`, `RetrieveOrder`, `UpdateOrder` …); `TransactionToken`, `OrderToken`, `SubscriptionToken`, `SavedCardToken` istek adları kalktı (ödemedeki `SavedCardToken` alanı yerinde).
- **Ödeme yanıtı iç içe:** `payment.TransactionToken` → `payment.Transaction.Token`, `payment.Status` → `payment.Transaction.Status`; iade ve iptalde `GiveBack.Type` / `Amount` → `GiveBack.Refund.Type` / `Amount`.
- **Enum'lar:** para birimi, dönem, durumlar, kart şeması ve tipi `string` değil `Odemehub.Enums` enum'larıdır; bilinmeyen değer `Unknown` okunur.
- **Müşteri:** `Customer { Reference, BillingAddress, ShippingAddress }` ve `Address`; eski düz müşteri alanları, `TaxDetails` ve istek tarafındaki `NamedCustomer` kalktı. Sipariş ve abonelik yanıtlarında müşteri hem üst seviyede hem kaydın üzerinde durur.
- **Kalemler:** katalog yok; `Item` adı, fiyatı, adedi ve KDV oranını kendisi taşır. `OrderItem` / `SubscriptionItem` yerine `Item`, gönderim için `ShippingMethod`.
- **Yanıt sınıfları:** `OrderDetails`, `OrderList`, `SubscriptionDetails`, `SubscriptionList`, `PaymentLinkDetails`, `PaymentLinkList`, `PaymentList`, `SavedCardDetails`, `SavedCardList`, `DeletedSavedCard`.
- **Hatalar:** bulunamayan kayıt artık `NotFoundException` (404; eskiden 422 `ValidationException`). `ForbiddenException` (403) ve `RateLimitException` (429) eklendi.
- **İstemci tarafı denetim yok:** kartla kayıtlı kartın birlikte verilmesi artık `ArgumentException` değil, geçitten 422'dir.
- **Webhook:** `OrderWebhook()`, `SubscriptionWebhook()`, `TransactionWebhook()` yerine tek `Webhook(method, path, body, timestamp, signature)` (ve `VerifyWebhook()`); imza istek ve yanıtlarla aynı şemadadır. Gövde yalnızca token taşır (`OrderToken`, `PaymentLinkToken`, `SubscriptionToken`, `TransactionToken`); durum `Retrieve*Async()` ile sorulur. Adresler panelde tanımlandığı için `SecurePayment`, `CreateOrder`, `UpdateOrder`, `CreateSubscription`, `UpdateSubscription` artık `WebhookUrl` almaz. `Signature`'ın yalnız gövdeyi imzalayan `Sign(body)` / `Verify(body, signature)` metotları kalktı.

## İmzayı elle doğrulamak

SDK imzayı `Odemehub.Signature` sınıfıyla üretir ve doğrular:

```csharp
var signature = new Signature(apiSecret);

signature.Sign("POST", "/api/1000000001/gateway/regular-payment", body, timestamp);
signature.Verify(method, path, body, request.Headers["X-Timestamp"], request.Headers["X-Signature"]);   // 5 dakikalık pencereyle
```

Test vektörü: `secret_test` anahtarıyla, `1700000000` anında, `/api/1000000001/gateway/regular-payment` yoluna `POST` edilen `{"a":1}` gövdesinin imzası `4d6225c9dd46837418b40dd8140d76a24cd7520d81ff3b280bf98da8da6a8771`'dir.
