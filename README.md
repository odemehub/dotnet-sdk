# ödemehub .NET SDK

ödemehub ödeme geçidini kendi uygulamanızdan kullanmak için hazırlanmış .NET istemcisi. Kart çekmek, 3D ödeme başlatmak, müşteriyi ödeme sayfasına yollamak, ürün kataloğunuzu eşlemek, abonelik açmak, kart saklamak, iade ve iptal yapmak ve bir kartın taksit seçeneklerini sormak için gereken her şey burada.

İstemci her isteği takımınızın gizli anahtarıyla imzalar, gelen her yanıtın imzasını doğrular. Siz imza, başlık ya da JSON ayrıntılarıyla uğraşmazsınız. Yalnızca .NET'in kendi `HttpClient`'ını ve `System.Text.Json`'ını kullanır; başka paket gerekmez.

## Kurulum

.NET 8 ve üzeri gerekir.

```bash
dotnet add package Odemehub.Sdk
```

## Yapılandırma

Dört bilgiye ihtiyacınız var. Hepsi paneldeki **Entegrasyon** sayfasındadır (menünün en altında): API anahtarı, gizli anahtar, Çalışma Alanı Kimliğiniz ve kanallarınızla ödeme hesaplarınızın token'ları.

```csharp
using Odemehub;

var client = new Client(new Options
{
    BaseUrl = "https://odeme.gurmehub.com",
    Team = "4829301756",                                   // Çalışma Alanı Kimliğiniz
    ChannelToken = "6f1c2e7a-4b3d-4c8e-9a61-2f5d7b0c3e14", // müşterinin size ulaştığı kanal
    ApiKey = Environment.GetEnvironmentVariable("ODEMEHUB_API_KEY")!,
    ApiSecret = Environment.GetEnvironmentVariable("ODEMEHUB_API_SECRET")!,
});
```

Gizli anahtar hiçbir zaman tel üzerinden gitmez; yalnızca imza üretmekte kullanılır. Anahtarları kodun içine yazmayın; ortam değişkeninde ya da gizli anahtar deposunda tutun.

Kanal token'ı entegrasyonun tamamı için bir kez verilir. Birden çok kanalda satıyorsanız tek bir istekte `ChannelToken` vererek o isteği başka kanala yazdırabilirsiniz. Geçit hiçbir yerde veritabanı numarası kullanmaz: kanal, ödeme hesabı, işlem, kayıtlı kart, abonelik ve sipariş her zaman token'ıyla adlanır.

İstemci durum tutmaz; uygulama boyunca tek bir nesneyi paylaşın (ASP.NET Core'da `builder.Services.AddSingleton(client)`). İstek bir dakika içinde yanıt almazsa kesilir; süreyi `Options.Timeout` ile değiştirebilirsiniz. Bütün metotlar `CancellationToken` alır. `IHttpClientFactory`'den aldığınız bir `HttpClient`'ı `new Client(options, httpClient)` ile verebilirsiniz.

İstekler `Odemehub.Requests` ad alanındaki nesnelerdir ve nesne başlatıcıyla kurulur. Zorunlu alanlar `required` işaretlidir; eksik bırakırsanız kod derlenmez. İsteğe bağlı bir alanı vermezseniz gövdeye hiç yazılmaz. Kart numarası ve güvenlik kodu `ToString()` çıktısında `*****` görünür; kart nesnesi yanlışlıkla loglansa da kart bilgisi görünmez.

İstek ve yanıt sınıfları aynı adları taşır (`Requests.SecurePayment` → `Responses.SecurePayment`). Yalnızca `Odemehub.Requests`'i `using` ile ekleyip yanıtları `var` ile karşılamak en rahatıdır.

## Karttan doğrudan çekim

Müşteriyi bankasına göndermeden çekim yapar. Başarılı yanıt, paranın alındığı anlamına gelir.

```csharp
using Odemehub.Requests;

var payment = await client.RegularPaymentAsync(new RegularPayment
{
    ChannelReference = "SIP-10231",          // işlemin sizdeki referansı
    Amount = "450.00",
    InstallmentNumber = 1,
    Ip = httpContext.Connection.RemoteIpAddress!.ToString(),
    Customer = new Customer
    {
        ChannelReference = "musteri-88",
        Firstname = "Ahmet",
        Lastname = "Yılmaz",
        Email = "ahmet@ornek.com",
        Phone = "05551112233",
        Address = "Kızılırmak Mah. Dumlupınar Blv. No:3",
        District = "Çankaya",
        Province = "Ankara",
        Country = "Türkiye",
    },
    Card = new Card
    {
        HolderName = "AHMET YILMAZ",
        Number = "5400360000000003",
        ExpiryMonth = "12",
        ExpiryYear = "2030",
        SecurityCode = "000",
    },
});

if (payment.Result.IsSuccessful)
{
    // payment.TransactionToken — ödemenin geçitteki token'ı; iade ve iptalde bununla adlandırılır
}
```

Tutarlar her zaman `string`'dir (`"450.00"`): imzalanıp gönderildiği gibi kalır, yolda yuvarlanmaz.

## 3D ödeme

3D'de çekim iki adımdır: siz ödemeyi başlatırsınız, müşteri bankasına gider, banka sonucu sizin adresinize gönderir.

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
    return Results.Redirect(payment.RedirectUrl!);   // müşteriyi bankaya gönderin
}
```

Başarılı yanıt **ödeme alındı demek değildir**; yalnızca müşterinin gideceği adres hazır demektir.

Müşteriyi **15 dakika içinde** bu adrese yönlendirin. Sayfası o süre içinde açılmayan ödemenin süresi dolar (`expired`). Süresi dolmuş bağlantıyı açan müşteri doğrudan `CallbackUrl` adresinize, `successful=0` ile geri gönderilir; `RetrievePaymentAsync()` sorgusu da başarısız sonucu ve nedenini döner.

Banka işini bitirince müşteri, tarayıcısı üzerinden `CallbackUrl` adresinize döner. O POST (form gövdesi) **sonucu taşımaz**, yalnızca sonucun hazır olduğunu haber verir:

| Alan | Anlamı |
| --- | --- |
| `transaction_token` | ödemenin geçitteki token'ı |
| `channel_reference` | sizin kendi referansınız |
| `successful` | `1` / `0` — yalnızca ipucu, **güvenilmez** |

Sonucu kendi imzalı bağlantınızdan sorun:

```csharp
app.MapPost("/odeme/donus", async ([FromForm(Name = "transaction_token")] string transactionToken, Client client) =>
{
    var outcome = await client.RetrievePaymentAsync(new RetrievePayment { TransactionToken = transactionToken });

    if (outcome.Result.IsSuccessful)
    {
        // siparişi ödendi olarak işaretleyin
    }
    // ...
}).DisableAntiforgery();
```

Neden böyle: o POST'u bizim sunucumuz değil, müşterinin tarayıcısı gönderir; tarayıcıya imzalayacak bir sır verilemez. `successful` alanına bakıp sipariş kapatmayın — onu herkes gönderebilir; yalnız "başarısız" ipucunda gereksiz sorgudan kaçınmak için kullanın. Geçide sorduğunuz yanıt ise her zaman imzalıdır ve SDK imzayı sizin için doğrular. Başkasının işlemini sorarsanız `ValidationException` alırsınız.

## Ürünler

Sipariş kalemleri ve abonelikler ürünleri **sizdeki referanslarıyla** adlandırır. Ürünü panelde (Ürünler sayfası) tanımlayabilir ya da kendi kataloğunuzdan geçide yazabilirsiniz:

```csharp
var product = await client.SaveProductAsync(new SaveProduct
{
    ChannelReference = "KAHVE-MAKINESI",
    Name = "Kahve makinesi",
    Type = "simple",          // simple | recurring
    Amount = "450.00",
    TaxRate = "20",           // fiyatın içindeki KDV oranı
});

await client.SaveProductAsync(new SaveProduct
{
    ChannelReference = "PREMIUM-AYLIK",
    Name = "Premium üyelik",
    Type = "recurring",
    Amount = "149.90",
    TaxRate = "20",
    Period = "monthly",       // monthly | yearly — yalnız recurring için zorunlu
});
```

Aynı kanalda aynı referans aynı üründür: tekrar gönderirseniz ikinci ürün açılmaz, mevcut olan güncellenir. `Currency` verilmezse TRY, `IsActive` verilmezse `true` kabul edilir. Ürün silinmez; `IsActive = false` ile satışa kapatılır.

Ödeme istekleri ürünü hiçbir zaman değiştirmez; ürünün tek yazıldığı yer bu çağrı ve panel.

## Ödeme sayfası

Kart bilgisini hiç görmek istemiyorsanız sipariş açıp müşteriyi geçidin kendi sayfasına yollayabilirsiniz.

```csharp
var order = await client.OrderPaymentAsync(new OrderPayment
{
    ChannelReference = "SIPARIS-10233",
    SuccessUrl = "https://magazam.com/tesekkurler",
    CancelUrl = "https://magazam.com/sepet",
    Customer = customer,
    Items =
    [
        new OrderItem { ChannelReference = "KAHVE-MAKINESI" },
        new OrderItem { ChannelReference = "KAHVE-500G", Quantity = 2, UnitAmount = "180.00" },
        new OrderItem { ChannelReference = "HEDIYE-PAKETI", Name = "Hediye paketi", UnitAmount = "25.00" },
    ],
});

return Results.Redirect(order.CheckoutUrl);
```

Sipariş tutarını göndermezsiniz; geçit kalemleri toplar ve `order.Amount` olarak döner. Bir kalemin boş bıraktığı ad, fiyat ve KDV oranı kayıtlı üründen gelir; kalemde verdiğiniz değerler yalnızca o sipariş için geçerlidir, ürünü değiştirmez. Kayıtlı olmayan bir referansla da kalem gönderebilirsiniz, ama o zaman `Name` ve `UnitAmount` zorunludur.

Ödeme tamamlanınca müşteri, 3D'dekiyle aynı biçimde `SuccessUrl` adresinize döner: aynı üç alan gelir, sonucu yine `RetrievePaymentAsync()` ile sorarsınız. Müşteri ödeme sayfasında karttan kaynaklı bir hata alırsa size dönmez, sayfada kalıp başka kartla dener.

## Abonelikler

Müşteriden dönem dönem tahsilat yapmak için abonelik açarsınız. Neye abone olunduğu bir ya da birkaç **abonelik ürünüdür** (`Type = "recurring"`), sizdeki referanslarıyla adlandırılır; fiyatı, para birimini ve dönemini ürün taşır. Aynı aboneliğe konan ürünlerin dönemi ve para birimi aynı olmalıdır.

```csharp
var subscription = await client.SubscriptionPaymentAsync(new SubscriptionPayment
{
    ChannelReference = "UYELIK-4471",
    Items =
    [
        new SubscriptionItem { ChannelReference = "PREMIUM-AYLIK" },
        new SubscriptionItem { ChannelReference = "EK-KULLANICI", Quantity = 3 },
    ],
    SuccessUrl = "https://magazam.com/tesekkurler",
    Customer = customer,
});

subscription.Token; // aboneliği sonra sorgulamak ve iptal etmek için saklayın

return Results.Redirect(subscription.CheckoutUrl!);
```

Bir kaleme `UnitAmount` verirseniz o fiyat **yalnızca ilk dönem** için geçerlidir (ör. ilk ay yarı fiyat); sonraki dönemler ürünün kendi fiyatından çekilir.

İlk ödeme her zaman geçidin kendi sayfasında yapılır ve kart zorunlu olarak saklanır: sonraki dönemler o karttan çekilir. Ödeme tamamlanınca müşteri `SuccessUrl` adresinize döner ve sonucu yine `RetrievePaymentAsync()` ile sorarsınız; abonelik `active` olur ve aşağıdaki bildirim de gider.

Dönem bitince yeni dönem açılır ve müşterinin varsayılan kartından çekilir. Banka kabul etmezse çekim bir buçuk gün içinde beş kez denenir (araları 3, 6, 9 ve 12 saat); bu sırada abonelik `active` kalır. Beşinci deneme de olmazsa abonelik `past_due` olur; çalışma alanı yöneticilerinize e-posta, `WebhookUrl` adresinize bildirim gider. İkisi de o dönemin dilediği kartla ödenebileceği bağlantıyı taşır; bağlantıyı müşterinize siz iletirsiniz. Süre sınırı yoktur; müşteri ödediği anda abonelik kaldığı yerden devam eder.

Aboneliğin durumunu sorabilirsiniz:

```csharp
var subscription = await client.RetrieveSubscriptionAsync(new RetrieveSubscription { SubscriptionToken = token });

subscription.Status;      // pending | active | past_due | cancelled
subscription.Amount;      // 149.90 — içinde bulunulan dönemin fiyatı
subscription.EndsAt;      // sonraki tahsilat zamanı
subscription.CheckoutUrl; // ödenmemiş dönem varsa müşteriye verilecek adres

foreach (var item in subscription.Items)
{
    Console.WriteLine($"{item.Quantity} x {item.Name} ({item.ChannelReference})");
}

if (subscription.IsPastDue)
{
    // müşteriyi kendi ödeme sayfanızda uyarabilirsiniz
}
```

Tutar, aboneliğin **içinde bulunduğu dönemin** fiyatıdır. Ürünün fiyatını yükseltirseniz yürüyen dönem çekildiği fiyatta kalır, yeni fiyat sonraki dönemden itibaren işler.

İptalde ödenmiş günler yanmaz:

```csharp
var subscription = await client.CancelSubscriptionAsync(new CancelSubscription { SubscriptionToken = token });

subscription.CancelledAt;  // iptal edildiği an
subscription.EndsAt;       // hizmetin süreceği son gün
subscription.IsCancelled;  // ödenmiş dönem sürüyorsa henüz false
```

Müşteri, ödediği dönemin sonuna kadar hizmeti almaya devam eder; o güne kadar abonelik `active` görünür, dönem bitince `cancelled` olur ve bir daha tahsilat yapılmaz. Ödenmemiş bir aboneliğin (ilk ödemesi yapılmamış ya da `past_due`) iptali hemen geçerlidir. İade yapılmaz.

Aboneliğin açılabilmesi için varsayılan ödeme hesabınızın kart saklayabiliyor olması gerekir; saklamayan bir hesapla açmaya çalışırsanız istek `subscription.payment_provider_token` alanında reddedilir.

### Abonelik bildirimleri (webhook)

Abonelik açarken `WebhookUrl` verirseniz, aboneliğin durumu her değiştiğinde o adrese imzalı bir POST gönderilir. Gövde düz JSON'dur ve imza `X-Signature` başlığındadır — yani geçidin API yanıtlarıyla aynı yöntem.

```csharp
var subscription = await client.SubscriptionPaymentAsync(new SubscriptionPayment
{
    ChannelReference = "UYELIK-4471",
    Items = [new SubscriptionItem { ChannelReference = "PREMIUM-AYLIK" }],
    SuccessUrl = "https://magazam.com/tesekkurler",
    Customer = customer,
    WebhookUrl = "https://magazam.com/odemehub/abonelik",
});
```

Bildirimi karşılayan uçta gövdeyi **ham** (`byte[]`) okuyup imzayla birlikte SDK'ya verin. İmza gövdenin bayt bayt kendisini kapsar; gövdeyi bir nesneye çevirip yeniden yazarsanız imza tutmaz.

```csharp
app.MapPost("/odemehub/abonelik", async (HttpRequest request, Client client) =>
{
    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer);

    Odemehub.Responses.SubscriptionWebhook webhook;

    try
    {
        webhook = client.SubscriptionWebhook(buffer.ToArray(), request.Headers["X-Signature"].FirstOrDefault());
    }
    catch (SignatureException)
    {
        return Results.BadRequest();
    }

    var subscription = webhook.Subscription;   // sorgudakiyle aynı nesne

    if (webhook.IsActive) AbonelikErisiminiAc(subscription.ChannelReference, subscription.EndsAt);
    else if (webhook.IsPastDue) MusteriyiUyar(subscription.CheckoutUrl);
    else if (webhook.IsCancelled) YenilemeyiDurdur(subscription.EndsAt);
    else if (webhook.IsEnded) ErisimiKapat(subscription.ChannelReference);

    return Results.Ok();
});
```

Gönderilen olaylar aboneliğin **durumudur**, yapılan işlem değil:

| Olay | Ne zaman gider |
| --- | --- |
| `active` | bir dönem ödendi (ilk ödeme ya da yenileme) |
| `past_due` | dönem kayıtlı karttan tahsil edilemedi, müşteriden bekleniyor |
| `cancelled` | abonelik iptal edildi; müşteri `EndsAt` tarihine kadar hizmeti almaya devam eder |
| `ended` | ödenmiş dönem doldu, abonelik kapandı |

2xx dışında bir yanıt (ya da yanıtsızlık) başarısız sayılır; bildirim 5 dakika sonra bir kez daha denenir. Ulaşmayan bildirimler panelde aboneliğin sayfasında HTTP kodu ve yanıtıyla listelenir.

## Ödeme hangi hesaptan geçer

`PaymentProviderToken` verirseniz ödeme o hesaptan geçer; sipariş ve abonelik açarken de aynı alan vardır ve müşteri ödeme sayfasında o hesaptan öder. Vermezseniz hesabı çalışma alanınız seçer: panelde **Ödeme Ayarları → Gate (Yönlendirme)** altındaki kurallar sırayla denenir ve ödemenin karşıladığı ilk kural hesabı belirler. Kurallar kartın bankasına, şemasına, programına, tipine, ticari kart olup olmadığına, tutara ve para birimine bakabilir. Hiçbir kural tutmazsa ödeme varsayılan hesaptan geçer.

- Kuralın hesabı ödemeyi alamıyorsa (ödeme türünü ya da para birimini desteklemiyorsa) o kural atlanır.
- Kayıtlı kartla ödeme her zaman kartın saklandığı hesaptan geçer.
- Taksitleri `RetrieveBinAsync()` ile gösteriyorsanız orada da hesap vermeyin: taksitler ödemenin gideceği hesaptan gelir ve çekilen tutar gösterdiğinizle aynı olur.

## Kur çevirisi

Panelde **Ödeme Ayarları → Kur Çevirici** altında bir kural tanımladıysanız, o para biriminde gelen ödeme karttan kuralın para biriminde çekilir. Örneğin 100 USD istersiniz, karttan 4.985,56 TRY çekilir. Kur, TCMB'nin güncel döviz satış kuru ve üzerine eklediğiniz marjdır ya da sizin girdiğiniz sabit kurdur.

İsteğinizde hiçbir şey değişmez: tutarı ve para birimini her zamanki gibi gönderirsiniz. Yanıttaki `Conversion` karttan ne çekildiğini söyler:

```csharp
var payment = await client.RegularPaymentAsync(new RegularPayment
{
    Amount = "100.00",
    Currency = "USD",
    // ...
});

if (payment.Conversion is not null)
{
    payment.Conversion.Amount;   // 4985.56
    payment.Conversion.Currency; // TRY
    payment.Conversion.Rate;     // 49.855560
}
```

- Çevrilmeyen ödemede `Conversion` `null` gelir. `RetrievePaymentAsync()` aynı bilgiyi yeniden verir.
- İade tutarını çekilen para biriminde gönderin (yukarıdaki örnekte TRY).
- Güncel kur alınamıyorsa ödeme alınmaz; `422` ile `transaction.currency` alanında hata döner. Birkaç dakika sonra tekrar deneyin.

## Kart sorgusu ve taksitler

Kart numarasının ilk hanelerinden kartın kim tarafından verildiğini, hangi programa ait olduğunu ve tutarın kaç taksite bölünebileceğini sorar. Hiçbir şey çekilmez.

```csharp
var bin = await client.RetrieveBinAsync(new RetrieveBin { Bin = "54003600", Amount = "450.00" });

if (bin.Result.IsSuccessful)
{
    bin.IssuerName;     // Garanti Bankası
    bin.Program;        // Bonus
    bin.Scheme;         // mastercard
    bin.Type;           // credit
    bin.IsCommercial;

    foreach (var installment in bin.Installments)
    {
        // 3 taksitte ayda 157.87, toplam 473.60
        Console.WriteLine($"{installment.Number} x {installment.Amount} = {installment.Total}");
    }
}
```

Yanıtta sorulan haneler `bin.Number` alanındadır (C#'ta bir özellik, içinde bulunduğu sınıfla aynı adı taşıyamaz).

Kartın tamamını göndermeyin; ilk 6-8 hane yeter ve yalnızca o kadarı kabul edilir.

Sorgu başarısız dönebilir: kart tanınmıyor olabilir ya da hesabınızın sağlayıcısı taksit vermiyor olabilir. İki durumda da satışı durdurmayın, tek çekimle devam edin.

## Tutarlar ve taksit

İki tutar vardır ve karıştırılmamalıdır:

| Alan | Anlamı |
| --- | --- |
| `Amount` | **Karttan çekilecek** tutar. Vade farkı varsa içindedir. |
| `BaseAmount` | **Sattığınız** tutar, vade farkından önceki hâli. Gönderilmezse `Amount` ile aynı kabul edilir. |

Taksit yalnızca Türk Lirası ödemelerde yapılır. USD, EUR ya da GBP ödemede `InstallmentNumber` `1` olmalıdır ve `RetrieveBinAsync()` taksit listesini boş döner; kur çevirisiyle TRY'den başka bir para birimine çekilen ödeme için de aynısı geçerlidir.

Taksitsiz satışta ikisi eşittir ve `BaseAmount` göndermenize gerek yoktur. Taksitli satışta `RetrieveBinAsync` size o taksidin toplamını verir; onu `Amount` olarak, sattığınız tutarı `BaseAmount` olarak gönderin:

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

Bazı sağlayıcılar vade farkını kendileri ekler; geçit bunu bilir ve gerekirse sağlayıcıya taban tutarı gönderir. Sizin tarafınızda değişen bir şey yoktur.

## Kayıtlı kartlar

Müşterinin kartını saklayıp sonraki ödemelerde numara sormadan çekim yapabilirsiniz.

```csharp
// Ödeme sırasında saklamak için: karta ShouldSave = true verin.
// Ödeme olmadan saklamak için:
var kept = await client.SaveCardAsync(new SaveCard { Customer = customer, Card = card });

var musteri = new NamedCustomer { ChannelReference = "musteri-88" };

// Müşterinin kartları
var cards = await client.SavedCardsAsync(new SavedCards { Customer = musteri });

// Varsayılan yapma / silme
var token = cards.SavedCards[0].Token;

await client.DefaultSavedCardAsync(new DefaultSavedCard { Customer = musteri, SavedCardToken = token });
await client.DeleteSavedCardAsync(new DeleteSavedCard { Customer = musteri, SavedCardToken = token });
```

Kayıtlı kartla ödeme alırken `Card` yerine kartın token'ını verin:

```csharp
var payment = await client.RegularPaymentAsync(new RegularPayment
{
    ChannelReference = "SIP-10235",
    Amount = "120.00",
    InstallmentNumber = 1,
    Ip = ip,
    Customer = customer,
    SavedCardToken = token,
});
```

`Card` ile `SavedCardToken` birlikte ya da hiçbiri verilmezse istek gönderilmeden `ArgumentException` fırlatılır.

Kart saklayan bir ödemenin yanıtında `payment.SavedCard` dolu gelir; kartın token'ını oradan öğrenirsiniz. Kart her yerde token ile adlandırılır.

## İade ve iptal

```csharp
// Gün sonu almamış ödemenin tamamını geri alır
await client.CancelPaymentAsync(new CancelPayment { TransactionToken = payment.TransactionToken });

// Tutar verilirse kısmi, verilmezse kalanın tamamı iade edilir.
// Kur çevirisiyle çekilen ödemede tutar çekilen para birimindedir.
await client.RefundPaymentAsync(new RefundPayment { TransactionToken = payment.TransactionToken, Amount = "100.00" });
```

## Hatalar

Bütün hatalar `OdemehubException`'dan türer; tek bir `catch` hepsini yakalar.

| Hata | Ne demek |
| --- | --- |
| `ValidationException` | Gönderdiğiniz alanlar kabul edilmedi. Ödeme denenmedi. `Errors` alan alan söyler. |
| `AuthenticationException` | API anahtarı bu takıma ait değil ya da imza gizli anahtarla tutmuyor. |
| `SignatureException` | Gelen yanıtın ya da bildirimin imzası tutmadı. Geçitten geldiği kanıtlanamaz; **işleme almayın**. |
| `TransportException` | Geçide ulaşılamadı, yanıt süresinde gelmedi ya da okunamadı. Ödemenin ne olduğu belirsizdir; geçitteki kayıt asıl doğruyu söyler. |
| `UnexpectedResponseException` | Beklenmeyen bir yanıt geldi. `Status` HTTP kodunu verir. |

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
    // exception.Message
}
```

Ağ hatasında ödemeyi körlemesine tekrarlamayın: `TransportException` "olmadı" demek değil, "bilmiyorum" demektir. Kendi `CancellationToken`'ınızla iptal ettiğiniz istek ise her zamanki gibi `OperationCanceledException` fırlatır.

## İmzayı elle doğrulamak

İmza, gövdenin tam metninin gizli anahtarla HMAC-SHA256'sıdır, küçük harf hex olarak yazılır. SDK bunu `Odemehub.Signature` sınıfıyla yapar:

```csharp
new Signature(apiSecret).Sign(body);
```

Test vektörü: `secret_test` anahtarıyla `{"a":1}` gövdesinin imzası `6d0c951564cdd2b6b70e75b214293a8cd2542815ba54fe91c7f6ce105bc3d592`'dir.
