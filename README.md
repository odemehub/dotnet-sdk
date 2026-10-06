# ödemehub .NET SDK

ödemehub ödeme geçidini kendi uygulamanızdan kullanmak için hazırlanmış .NET istemcisi. Kart çekmek, 3D ödeme başlatmak, sipariş, abonelik ve ödeme linki açmak, kart saklamak, iade ve iptal yapmak, taksit sormak: hepsi burada.

İstemci her isteği gizli anahtarınızla imzalar, gelen her yanıtın imzasını doğrular. Siz imza, başlık ya da JSON ayrıntılarıyla uğraşmazsınız. Her uç nokta için bir metot vardır ve adı uç noktanın adıdır: `create-order` için `CreateOrderAsync()`, `retrieve-saved-cards` için `RetrieveSavedCardsAsync()`. Yalnızca .NET'in kendi `HttpClient`'ını ve `System.Text.Json`'ını kullanır; başka paket gerekmez.

## Kurulum

.NET 8 ve üzeri gerekir.

```bash
dotnet add package Odemehub.Sdk
```

## Yapılandırma

Üç bilgi gerekir. Hepsi paneldeki **Entegrasyon** sayfasındadır (menünün en altında): Çalışma Alanı Kimliğiniz, API anahtarı ve gizli anahtar.

```csharp
using Odemehub;

var client = new Client(new Options
{
    BaseUrl = "https://app.odemehub.com",
    Team = "1000000001",                                   // Çalışma Alanı Kimliği
    ApiKey = Environment.GetEnvironmentVariable("ODEMEHUB_API_KEY")!,
    ApiSecret = Environment.GetEnvironmentVariable("ODEMEHUB_API_SECRET")!,
});
```

Gizli anahtar hiçbir zaman tel üzerinden gitmez; yalnızca imza üretmekte ve doğrulamakta kullanılır. Anahtarları kodun içine yazmayın; ortam değişkeninde ya da gizli anahtar deposunda tutun.

Geçit hiçbir yerde veritabanı numarası kullanmaz: ödeme hesabı, işlem, sipariş, abonelik, link ve kayıtlı kart her zaman token'ıyla anılır.

İstemci durum tutmaz; uygulama boyunca tek bir nesneyi paylaşın (ASP.NET Core'da `builder.Services.AddSingleton(client)`). İstek bir dakika içinde yanıt almazsa kesilir; süreyi `Options.Timeout` ile değiştirebilirsiniz. Bütün metotlar `CancellationToken` alır. `IHttpClientFactory`'den aldığınız bir `HttpClient`'ı `new Client(options, httpClient)` ile verebilirsiniz.

İstekler `Odemehub.Requests` ad alanındaki nesnelerdir ve nesne başlatıcıyla kurulur. Zorunlu alanlar `required` işaretlidir; eksik bırakırsanız kod derlenmez. İsteğe bağlı bir alanı vermezseniz gövdeye hiç yazılmaz. SDK alanları kendisi denetlemez; kuralları geçit uygular ve hatayı alan alan `ValidationException` ile döner. Kart numarası ve güvenlik kodu `ToString()` çıktısında `*****` görünür; kart nesnesi yanlışlıkla loglansa da kart bilgisi görünmez.

Yanıtlar `Odemehub.Responses` ad alanındadır ve geçidin JSON'unu birebir yansıtır: `payment.Transaction.Status`, `order.Order.Customer` gibi. Para birimi, dönem ve durumlar `Odemehub.Enums` altındaki enum'lardır (`Currency`, `Period`, `OrderStatus`, `SubscriptionStatus`, `LinkPaymentStatus`, `TransactionStatus`, `PaymentStatus`, `SecurityType`, `RefundType`, `RefundStatus`, `CardScheme`, `CardType`, ödeme linki için `AmountType`, `CurrencyType`, `TaxMode`). Geçit bu sürümün tanımadığı yeni bir değer gönderirse yanıt yine okunur ve değer `Unknown` olur. `Odemehub.Requests` ile `Odemehub.Enums`'u `using` ile ekleyip yanıtları `var` ile karşılamak en rahatıdır.

## İmza

Her istek üç başlıkla gider: `X-Api-Key`, `X-Timestamp` (Unix saniye) ve `X-Signature`. İmza, `"{timestamp}\n{METHOD}\n{path}\n{body}"` metni üzerinden gizli anahtarla alınan HMAC-SHA256'nın küçük harfli hex hâlidir. `path` adresin sorgu dizesiz yolu (`/api/1000000001/gateway/regular-payment`), `body` gönderilen JSON'ın kendisidir. Bütün uç noktalar POST'tur. Zaman damgası sunucu saatinden 5 dakikadan uzak olamaz; sunucunuzun saati kaymışsa istekler `AuthenticationException` ile döner. Geçit her yanıtı aynı yöntemle imzalar; istemci yanıtı isteğin metodu ve yoluyla, yanıtın kendi `X-Timestamp` değeriyle doğrular. Test vektörü en alttadır.

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
    Reference = "SIP-10231",                  // sizdeki referans; en az bir rakam içermeli
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

**Müşteriler.** `Customer.Reference` gönderdiğiniz ödeme başarılı olunca geçit müşteriyi o referansla çalışma alanınızın müşteri listesine yazar ya da günceller; başarısız ödeme müşteriye dokunmaz. Referans göndermezseniz ödeme yine alınır ama müşteri kaydedilmez ve kart saklanamaz. Saklanan kart müşteriye bağlanır; müşterinin son ödeme yaptığı kart varsayılan kartı olur. Kayıtlı kartla ödemede `Customer.Reference` kartın saklandığı referansla aynı olmalıdır.

Kayıtlı kartla ödemede `Card` yerine `SavedCardToken` verilir; ödeme kartın saklandığı hesaptan geçer, `PaymentProviderToken` gönderilmez. Kart hangi müşteri referansıyla saklandıysa ödeme de aynısını taşımalıdır.

Tutarlar her zaman `string`'dir ve nokta ayraçlı, en çok iki ondalıklıdır: `"100"`, `"100.1"`, `"100.10"`; imzalanıp gönderildiği gibi kalır, yolda yuvarlanmaz. Tutar sıfırdan büyük ve en çok 10.000.000,00 olabilir. `Currency` (`Currency.TRY`, `USD`, `EUR`, `GBP`) boş bırakılırsa TRY'dir.

## 3D ödeme

Siz ödemeyi başlatırsınız, müşteri bankasına gider, banka müşteriyi sizin adresinize geri yollar.

```csharp
var payment = await client.SecurePaymentAsync(new SecurePayment
{
    Reference = "SIP-10232",
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

Banka işini bitirince müşterinin tarayıcısı `CallbackUrl` adresinize şu alanları POST eder: `transaction_token`, `reference`, `successful` (`1` / `0`). Bu POST imzasızdır ve müşterinin tarayıcısından gelir; yalnızca ipucudur. Sonucu kendi imzalı bağlantınızdan sorun:

```csharp
app.MapPost("/odeme/donus", async ([FromForm(Name = "transaction_token")] string transactionToken, Client client) =>
{
    var payment = (await client.RetrievePaymentsAsync(new RetrievePayments { Token = transactionToken })).Payments.FirstOrDefault();

    if (payment?.IsSuccessful == true)
    {
        // siparişi ödendi olarak işaretleyin
    }

    // payment.Status: TransactionStatus.Successful, Failed, Expired ...
    // payment.PaymentStatus: paranın akıbeti (sonradan Refunded olabilir)
    // payment.Amount: karttan çekilen tutar
    // ...
}).DisableAntiforgery();
```

`successful` alanına bakıp sipariş kapatmayın — onu herkes gönderebilir. Geçide sorduğunuz yanıt ise her zaman imzalıdır ve SDK imzayı sizin için doğrular. Başkasının ya da hiç olmayan bir işlemi sorarsanız liste boş döner.

Geçidin ödeme yanıtları (`SecurePaymentAsync`, `RegularPaymentAsync`, `RefundPaymentAsync`, `CancelPaymentAsync`) aynı şekli döner: `Result`, `Transaction` (`Token`, `Reference`, `Status`, `PaymentStatus`, `SecurityType`, `Amount`, `BaseAmount`, `Currency`, `InstallmentNumber`, `IsTest`, `CreatedAt`, ödeme bir siparişte, linkte ya da abonelikte alındıysa `OrderToken` / `PaymentLinkToken` / `SubscriptionToken`; linkte alınan ödemede `PaymentLinkToken` ile birlikte ödeyenin link ödemesini adlandıran `LinkPaymentToken`), `Customer` (`Reference` ve `BillingAddress`), `Conversion`, kart saklandıysa `SavedCard`; iade ve iptalde ayrıca `Refund`, 3D'de `RedirectUrl`.

Müşteri bankadan sonra sekmeyi kapatırsa tarayıcı `CallbackUrl` adresinize hiç dönmez; bunun için panelde `transaction.*` webhook'u tanımlayın (bkz. [Webhook](#webhook)). Bildirim hiç gelmezse `RetrievePaymentsAsync()` ile referansla sorabilirsiniz.

## Sipariş

Kart sizde sorulmaz. Siparişi açarsınız, geçit kendi ödeme sayfasının adresini döner, müşteri orada öder. Tutar gönderilmez: geçit kalemleri ve ödeyenin panelinizdeki listeden seçtiği gönderim yöntemini toplar. Birim tutarlar KDV dahildir (120 ve %20 → 100 mal + 20 KDV).

```csharp
var created = await client.CreateOrderAsync(new CreateOrder
{
    Reference = "SIP-10233",
    SuccessUrl = "https://magazam.com/odeme/donus",
    Items =
    [
        new Item { Name = "Kulaklık", UnitAmount = "1200.00", Quantity = 1, TaxRate = "20", Reference = "SKU-1", SaveAsProduct = true },
        new Item { Name = "Hediye paketi", UnitAmount = "25.00", Quantity = 1, TaxRate = "20", Image = "https://magazam.com/img/paket.jpg" },
    ],
    Customer = customer,                                   // bilinen kadarı; kalanı sayfada sorulur. Hiç verilmeyebilir.
    RequiresShipping = true,                               // ödeyen adresini ve gönderim yöntemini sayfada seçer
    CancelUrl = "https://magazam.com/sepet",
});

return Results.Redirect(created.Order.CheckoutUrl!);       // müşteriyi buraya gönderin
```

Siparişte müşterinin her parçası isteğe bağlıdır: `Reference`, `BillingAddress`, `ShippingAddress` ya da hiçbiri. Verilenler ödeme sayfasında dolu gelir, kalanı ödeyene sorulur. Referans verilmezse ödeyen müşteri listenize yazılmaz. `SaveAsProduct = true` olan kalem referansıyla ürün listenize yazılır (referans zorunlu); `TaxRate` verilmezse kalem vergisizdir. Gönderim yöntemleri istekte gönderilmez: panelinizdeki **Gönderim Yöntemleri** listesinden ödeyenin adresine uyanlar sunulur.

Yanıttaki `Order` siparişi bütünüyle taşır: `Token`, `Reference`, `Description`, `PaymentProviderToken`, `Status` (`OrderStatus.Open` / `Paid`), `Items`, `ShippingMethod` (ödeyenin seçtiği), `Discount`, `Subtotal`, `ShippingAmount`, `TaxAmount`, `Amount`, `Currency`, `IsTest`, `CreatedAt`, `CheckoutUrl` (ödenince `null`), `Transaction` (ödeyen işlem; açıkken `null`) ve `Customer`. Müşteri yanıtın üst seviyesinde de durur: `created.Customer` ile `created.Order.Customer` aynıdır; listelerde her siparişin kendi `Customer`'ı vardır.

**İndirim.** Kupon API'den gönderilmez; ödeyen kodu ödeme sayfasında girer. Kupon kullanıldıysa `Order.Discount` kodu (`Code`) ve kalemlerden düşülen tutarı (`Amount`) taşır, kullanılmadıysa `null`'dır. Siparişin `Subtotal`, `TaxAmount` ve `Amount` değerleri indirim düşülmüş hâlidir; kupon gönderim ücretinden düşülmez.

Ödendiğinde müşteri `SuccessUrl` adresinize 3D dönüşüyle aynı alanlarla POST edilir; kesin sonucu `RetrieveOrdersAsync()` verir; `order.paid` webhook'u geldiğinde de onu çağırın. `Order.Transaction.PaymentStatus` sonradan yapılan iadeyi gösterir.

```csharp
var current = (await client.RetrieveOrdersAsync(new RetrieveOrders { Token = token })).Orders[0];

if (current.IsPaid)
{
    // current.Transaction?.Token ile iade / iptal / RetrievePaymentsAsync yapılabilir
}

// Açık siparişte yalnızca gönderilen alanlar değişir; kalemler gönderilirse tamamı yenilenir.
await client.UpdateOrderAsync(new UpdateOrder { Token = token, Description = "Hediye paketi", Clear = ["cancel_url"] });
```

`Clear`, bir alanı boşaltmak içindir (alanı göndermemek eskisini korur). Gönderilen müşteri referansı eskisinin yerine geçer; ödenmiş siparişe hiçbir şey yazılmaz.

**Referans tekil değildir.** Her `Create*` çağrısı yeni bir kayıt ve yeni bir token açar; aynı referans daha önce gönderilmiş olsa da eski kayıt değişmez ve istek reddedilmez. Böylece 3D'de vazgeçen ödeyeni yeni bir siparişle yeniden ödemeye gönderebilirsiniz. Her yanıttaki token'ı kendi kaydınızda saklayın; var olan kaydı o token ile `Update*` çağrısı değiştirir. **Bekleyen ödeme varken sipariş ve abonelik güncellenemez:** ödeme sayfasında son 15 dakika içinde başlamış bir ödeme varsa `UpdateOrderAsync` ve `UpdateSubscriptionAsync` `token` alanında reddedilir. Link güncellemesini bekleyen ödeme engellemez.

## Ödeme linki

Link, adresi bilen herkesin ödeyebileceği bir sayfadır; kapatılana ya da son gününe kadar tekrar tekrar ödenir. Müşterisi yoktur; ödeyen kim olduğunu sayfada söyler.

```csharp
var link = await client.CreatePaymentLinkAsync(new CreatePaymentLink
{
    Items = [new Item { Name = "Kulaklık", UnitAmount = "1200.00", Quantity = 1, TaxRate = "20" }],
    Currency = Currency.TRY,
    Reference = "LNK-1",                 // boş bırakılırsa geçit LINK{n} üretir; tekil değildir
    ExpiresAt = "2026-12-31",            // çalışma alanının saat dilimine göre son gün
    EmailsPayer = true,                  // ödeme tamamlanınca ödeyene e-posta gider
});

link.PaymentLink.CheckoutUrl;            // linkin kendisi; ödenemezken (kapalı, süresi geçmiş) null
link.PaymentLink.ExpiresAt;              // verilen günün sonu (çalışma alanı saatiyle), ISO 8601 UTC
link.PaymentLink.IsActive;               // açık ve süresi geçmemiş mi
link.PaymentLink.IsTest;                 // ödemeleri şu an test ortamında mı alınıyor

var detail = (await client.RetrievePaymentLinksAsync(new RetrievePaymentLinks { Token = link.PaymentLink.Token })).PaymentLinks[0];
detail.Transactions;                     // son 50 deneme, yeniden eskiye
detail.TransactionsCount;                // linkteki denemelerin tamamının sayısı
detail.Successful;                       // listelenenlerden başarılı olanlar

await client.UpdatePaymentLinkAsync(new UpdatePaymentLink { Token = link.PaymentLink.Token, IsActive = false });
```

Panelden açtığınız linkler de aynı uçlarla bulunur. Linkle ödeyen kişi müşteri listenize yazılmaz ve kartı saklanmaz. Süresi geçmiş bir link yeni bir `ExpiresAt` verilince (ya da son günü `Clear = ["expires_at"]` ile kaldırılınca) yeniden ödeme alır. Her `CreatePaymentLinkAsync` çağrısı yeni bir link açar; dönen token'ı saklayın.

**Tutarı ödeyen seçer.** `AmountType` linkin neyle ödendiğini söyler: `Fixed` (varsayılan) kalemlerle; `Custom` ödeyenin yazdığı tutarla; `Predefined` sizin verdiğiniz tutarlardan biriyle; `PredefinedAndCustom` ikisinden biriyle. Seçimli tiplerde `Items` gönderilmez (gönderilirse yok sayılır); ödeme `ItemName` adlı tek kalem olarak alınır ve `ItemName` zorunludur. Hazır tutarlar `PredefinedAmounts` ile en çok 10 tane verilir. `TaxRate` bu tutarın KDV oranıdır; `TaxMode` `Inclusive` (varsayılan, KDV tutarın içinde) ya da `Exclusive` (KDV tutarın üstüne eklenir) olur.

**Para birimini ödeyen seçer.** `CurrencyType = CurrencyType.Selectable` ile ödeyen `Currency` ve `Currencies` içindekilerden birini seçer; `Currencies` bu durumda zorunludur. Varsayılan `Fixed`'dir: link yalnızca `Currency` ile ödenir.

```csharp
var donation = await client.CreatePaymentLinkAsync(new CreatePaymentLink
{
    Currency = Currency.TRY,
    AmountType = AmountType.PredefinedAndCustom,
    ItemName = "Bağış",
    PredefinedAmounts = ["100.00", "250.00", "500.00"],
    TaxRate = "0",
    CurrencyType = CurrencyType.Selectable,
    Currencies = [Currency.USD, Currency.EUR],
});

donation.PaymentLink.Amount;             // seçimli tipte null; Subtotal ve TaxAmount da
donation.PaymentLink.Currencies;         // [TRY, USD, EUR]; Fixed'de null

// Kalemli linke dönerken kalem göndermek zorunludur
await client.UpdatePaymentLinkAsync(new UpdatePaymentLink
{
    Token = donation.PaymentLink.Token,
    AmountType = AmountType.Fixed,
    Items = [new Item { Name = "Bağış", UnitAmount = "100.00", Quantity = 1 }],
});
```

Yanıttaki `PaymentLink` gönderilen alanların hepsini taşır: `AmountType`, `ItemName`, `PredefinedAmounts`, `TaxRate`, `TaxMode`, `CurrencyType`, `Currencies`, `EmailsPayer`. Tipin kullanmadığı alanlar geçitte boşaltılır (`null` döner). Güncellemede `Clear` `expires_at`, `description`, `payment_provider_token`, `item_name`, `predefined_amounts`, `tax_rate` ve `currencies` alanlarını boşaltabilir.

**Link ödemeleri.** Bir ödeyenin linki bir kez ödemesi bir link ödemesidir (`LinkPayment`); ödeyen ödemeye başlayınca açılır, bankası reddettikçe `LinkPaymentStatus.Open` kalır, ödeme geçince `Paid` olur. Referansı `LINKPAY{n}` biçimindedir ve link ödemesinin denemeleri de bu referansla görünür. Kimin ne ödediğini `RetrieveLinkPaymentsAsync()` verir:

```csharp
var payments = await client.RetrieveLinkPaymentsAsync(new RetrieveLinkPayments { CreatedFrom = "2026-10-01", CreatedTo = "2026-10-06" });

foreach (var linkPayment in payments.LinkPayments)
{
    // linkPayment.PaymentLink.Token / .Reference — ödendiği link
    // linkPayment.Status: LinkPaymentStatus.Open | Paid
    // linkPayment.Items, Subtotal, TaxAmount, Amount (indirim düşülmüş), Discount, Currency
    // linkPayment.Customer?.BillingAddress — ödeyenin sayfada girdiği fatura adresi
    // linkPayment.Transaction?.Token — ödeyen işlem; iade ve iptal bununla yapılır
}
```

## Abonelik

İlk yenileme ödeme sayfasında ödenir ve kart orada müşteriye saklanır; sonrakiler müşterinin varsayılan kartından (son ödeme yaptığı kart) çekilir. Müşteri referansı zorunludur, çünkü kart o müşteriye bağlanır. Ödeme hesabı (verilen ya da varsayılan) kart saklamalı ve 3D ödeme almalı, planınız kayıtlı kartları kapsamalıdır.

```csharp
var opened = await client.CreateSubscriptionAsync(new CreateSubscription
{
    Reference = "ABO-1",
    Period = Period.Monthly,             // Daily | Weekly | Monthly | Annually
    SuccessUrl = "https://magazam.com/abonelik/donus",
    Items = [new Item { Name = "Premium", UnitAmount = "99.90", Quantity = 1, TaxRate = "20" }],
    Customer = customer,
    RenewalLimit = 12,                   // boş: iptale kadar
});

return Results.Redirect(opened.Subscription.CheckoutUrl!);
```

```csharp
var subscription = (await client.RetrieveSubscriptionsAsync(new RetrieveSubscriptions { Token = token })).Subscriptions[0];

subscription.Status;            // SubscriptionStatus.Pending | Active | PastDue | Cancelled | Completed
subscription.Renewal.Amount;    // içinde bulunulan yenilemenin tutarı
subscription.Renewal.PaidAt;    // ödendiyse ne zaman
subscription.Renewal.EndsAt;    // ödenen dönemin sonu
subscription.NextPaymentAt;     // sonraki tahsilat zamanı
subscription.RenewalsPaid;      // şimdiye kadar ödenen yenileme sayısı
subscription.CheckoutUrl;       // ödenmemiş yenileme varsa müşteriye verilecek adres
subscription.Customer?.Reference;

// Dönem, kalemler, ödeme sayısı değişir; iptal de buradan:
await client.UpdateSubscriptionAsync(new UpdateSubscription { Token = token, Status = SubscriptionStatus.Cancelled });
```

`Subscription.Discount`, ödeyenin ilk ödemede ödeme sayfasında girdiği kuponu taşır (yoksa `null`); kupon yalnız ilk ödemeye uygulanır. Aboneliğin kendi `Subtotal` / `TaxAmount` / `Amount` değerleri indirimsizdir; ilk yenilemede çekilen tutar `Renewal.Amount`'tadır.

Kalemler gönderilirse ödenmemiş yenilemeye ve sonrakilere yansır. İptalde para iade edilmez; ödenmiş dönem sonuna kadar sürer, sonra abonelik biter. Ödenmiş dönem yoksa iptal hemen geçerlidir. `RenewalLimit` şimdiye kadar ödenen yenilemelerin altına inemez; sınırı kaldırmak için `Clear = ["renewal_limit"]`.

İlk ödemeden sonra yalnızca iptal (`Status`), ödeme sayısı (`RenewalLimit`), dönem (`Period`) ve aynı kalemlerin birim fiyatı değişebilir; müşteri dahil başka bir alan gönderilirse geçit 422 ile reddeder.

Dönem bitince yeni yenileme açılır ve müşterinin varsayılan kartından çekilir. Banka kabul etmezse çekim birkaç kez yeniden denenir; hiçbiri olmazsa abonelik `past_due` olur ve `CheckoutUrl` müşterinin kendisinin ödeyeceği adresi taşır.

## Webhook

Sipariş ödendiğinde, link ödemesi alındığında, abonelik durum değiştirdiğinde, API ödemesi bittiğinde ve bir ödeme iade ya da iptal edildiğinde geçidin **kendi sunucusu** imzalı bir JSON POST gönderir. Müşteri sekmeyi kapatıp `CallbackUrl` / `SuccessUrl` adresinize hiç dönmese de bu bildirim gelir. Adresler kodda verilmez; panelde **Ayarlar → Webhook** sayfasında olay ve adres seçilerek tanımlanır.

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
        var order = (await client.RetrieveOrdersAsync(new RetrieveOrders { Token = webhook.OrderToken })).Orders[0];
        // order.Status == OrderStatus.Paid, order.Transaction?.PaymentStatus == PaymentStatus.Refunded ...
    }
    else if (webhook.SubscriptionToken is not null)
    {
        var subscription = (await client.RetrieveSubscriptionsAsync(new RetrieveSubscriptions { Token = webhook.SubscriptionToken })).Subscriptions[0];
    }
    else if (webhook.LinkPaymentToken is not null)   // payment_link.*
    {
        var linkPayment = (await client.RetrieveLinkPaymentsAsync(new RetrieveLinkPayments { Token = webhook.LinkPaymentToken })).LinkPayments[0];
        // linkPayment.PaymentLink.Token == webhook.PaymentLinkToken, linkPayment.IsPaid, linkPayment.Transaction?.PaymentStatus ...
    }
    else if (webhook.TransactionToken is not null)   // transaction.*
    {
        var transaction = (await client.RetrievePaymentsAsync(new RetrievePayments { Token = webhook.TransactionToken })).Payments[0];
    }

    return Results.NoContent();
});
```

`payment_link.*` olaylarında gövde linkin token'ının yanında ödeyenin link ödemesini (`LinkPaymentToken`) da taşır. Abonelik ve link ödemelerinin iade/iptal olaylarında `TransactionToken` da gelir; `RetrievePaymentsAsync()` yanıtındaki `OrderToken` / `PaymentLinkToken` / `LinkPaymentToken` / `SubscriptionToken` ödemenin gerçekten o kaynağa ait olduğunu gösterir. Gövde `Discount` taşımaz; indirimi ilgili `Retrieve*Async()` yanıtı verir. Yalnızca doğrulamak için `client.VerifyWebhook(...)` `bool` döner. 2xx dışında bir yanıt (ya da yanıtsızlık) başarısız sayılır; geçit 60 sn, 5 dk, 15 dk ve 30 dk arayla toplam 5 kez dener ve yönlendirmeleri izlemez.

## Kayıtlı kartlar

Kart ödeme sırasında (`ShouldSave = true`) ya da ödemesiz saklanır; ikisinde de `Customer.Reference` zorunludur. Kart o müşteriye bağlanır ve müşteri referansıyla bulunur; kart yanıtlarındaki `Customer` yalnızca `Reference` taşır. Ödemesiz saklamada müşteri, sağlayıcı kartı kabul edince gönderdiğiniz bilgilerle listenize yazılır.

```csharp
var saved = await client.CreateSavedCardAsync(new CreateSavedCard { Customer = customer, Card = card });
saved.SavedCard?.Token;                  // sağlayıcı saklamadıysa null, nedeni Result.Message

var cards = await client.RetrieveSavedCardsAsync(new RetrieveSavedCards { Reference = "musteri-88" });   // müşterinin referansı
cards.Default;                           // varsayılan kart, varsa

await client.RetrieveSavedCardsAsync(new RetrieveSavedCards { Token = cardToken });
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
    Reference = "SIP-10234",
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

## Sorgulama

Her kaynak tek bir `Retrieve*Async` metoduyla sorulur ve yanıt her zaman bir listedir (eskiden yeniye); eşleşen yoksa boş liste döner. Kayıt üç yoldan biriyle adlandırılır: `Token`, sizdeki `Reference` ya da açıldığı günler (`CreatedFrom` / `CreatedTo`). Aralık en çok 7 gündür, `YYYY-MM-DD` biçimindedir ve çalışma alanının saat dilimindedir; hiçbiri verilmezse son 7 gün.

```csharp
// Yanıtı alınamayan bir ödemenin akıbeti: referanstaki bütün denemeler
await client.RetrievePaymentsAsync(new RetrievePayments { Reference = "SIP-10231" });

// Belli günlerdeki bütün denemeler, reddedilenler dahil, durumu ve tutarıyla
var list = await client.RetrievePaymentsAsync(new RetrievePayments
{
    CreatedFrom = "2026-09-26",
    CreatedTo = "2026-10-02",
});

foreach (var transaction in list.Payments)
{
    // transaction.Status: TransactionStatus; Timeout: sağlayıcı yanıt vermedi
    // transaction.PaymentStatus: PaymentStatus.Paid, Refunded, PartiallyRefunded ...
    // transaction.OrderToken / PaymentLinkToken / LinkPaymentToken / SubscriptionToken: bağlı olduğu kaynak, varsa
}

await client.RetrievePaymentsAsync(new RetrievePayments());   // son 7 gün
```

Aynısı `RetrieveOrdersAsync` (`RetrieveOrders`), `RetrieveSubscriptionsAsync` (`RetrieveSubscriptions`), `RetrievePaymentLinksAsync` (`RetrievePaymentLinks`), `RetrieveLinkPaymentsAsync` (`RetrieveLinkPayments`; `Reference` `LINKPAY{n}` biçimindedir) ve `RetrieveSavedCardsAsync` (`RetrieveSavedCards`; `Reference` müşterinin referansıdır) için de geçerlidir. Sipariş ve abonelik listelerinde her kaydın `Customer`'ı da gelir. Referans tekil olmadığından referansla sorgu aynı referanslı bütün kayıtları döner.

## Hatalar

Bütün hatalar `OdemehubException`'dan türer; tek bir `catch` hepsini yakalar.

| Hata | Durum | Anlamı |
| --- | --- | --- |
| `AuthenticationException` | 401 | API anahtarı yanlış, imza tutmuyor ya da zaman damgası aralık dışında |
| `ForbiddenException` | 403 | Çalışma alanı işlem yapamıyor (ödenmemiş bakiye, plan) ya da plan bu özelliği kapsamıyor |
| `NotFoundException` | 404 | Güncellenmek, silinmek, iade ya da iptal edilmek istenen kayıt yok (sorgularda boş liste döner) |
| `ValidationException` | 422 | Alan hataları; `Errors` noktalı alan adıyla (`transaction.amount`, `order.items.0.name`) |
| `RateLimitException` | 429 | İstek sınırı; `RetryAfter` saniye |
| `SignatureException` | — | Yanıtın ya da bildirimin imzası doğrulanamadı; içeriğe güvenmeyin |
| `TransportException` | — | Geçide ulaşılamadı ya da yanıt süresinde gelmedi; ödemenin akıbetini `RetrievePaymentsAsync` ile referansla sorun |
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

## 1.0.2'deki değişiklikler

Yeni:

- **Link ödemeleri:** `RetrieveLinkPaymentsAsync()` (`RetrieveLinkPayments`), `LinkPayment`, `LinkPaymentList`, `PaymentLinkReference` yanıtları ve `LinkPaymentStatus` enum'u. `PaymentTransaction`, `Transaction` ve `Webhook` linkte alınan ödemede `LinkPaymentToken` taşır.
- **Ödeme linki alanları:** `CreatePaymentLink` ve `UpdatePaymentLink` isteklerine `AmountType`, `ItemName`, `PredefinedAmounts`, `TaxRate`, `TaxMode`, `CurrencyType`, `Currencies`, `EmailsPayer` geldi; aynı alanlar `PaymentLink` yanıtında da var. Yeni enum'lar `AmountType`, `CurrencyType`, `TaxMode`. `UpdatePaymentLink.Clear` `item_name`, `predefined_amounts`, `tax_rate` ve `currencies` alanlarını da boşaltır.
- **İndirim:** `Order`, `Subscription` ve `LinkPayment` yanıtlarında `Discount { Code, Amount }` (kupon yoksa `null`). Webhook gövdesinde yoktur.

Küçük kırıcı değişiklikler:

- **`Create*` artık idempotent değil:** aynı referansla her çağrı yeni kayıt ve yeni token açar; eski kayıt yeniden yazılmaz, tekrarlanan referans 422 dönmez. Kodunuz kaydı referansla bulmaya dayanıyorsa her yanıttaki token'ı saklayın.
- **Bekleyen ödeme yalnız sipariş ve abonelik güncellemesini engeller;** `CreatePaymentLinkAsync` ve `UpdatePaymentLinkAsync` artık reddedilmez.
- **`CreatePaymentLink.Items` zorunlu değil** (`required` kalktı); seçimli tutarlı linkte gönderilmez.
- **`PaymentLink.Subtotal`, `TaxAmount`, `Amount` artık `string?`:** seçimli tutarlı linkte `null` gelir (eskiden `""` okunurdu).
- **`CreatePaymentLink` ve `UpdatePaymentLink` ortak alanlarını** yeni `PaymentLinkMessage` taban sınıfından alır; nesne başlatıcıyla yazılan kod değişmez.

## 1.0.1'deki kırıcı değişiklikler

1.0.1, SDK'yı geçidin bugünkü API'sine taşır ve 1.0.0 koduyla uyumlu değildir. 1.0.0'dan geçerken dikkat edilecekler:

- **İmza:** `X-Timestamp` başlığı geldi; imza artık zaman damgası, metot, yol ve gövde üzerinden alınır (yukarıda).
- **Uçlar:** `OrderPaymentAsync`, `SubscriptionPaymentAsync`, `SaveProductAsync`, `CancelSubscriptionAsync`, `RetrieveTransactionsAsync`, `SaveCardAsync`, `SavedCardsAsync`, `DefaultSavedCardAsync` kalktı. Yerlerine `Create*` / `Retrieve*` / `Update*` ailesi geldi (sipariş, ödeme linki, abonelik, kayıtlı kart). Abonelik iptali `UpdateSubscriptionAsync` ile `Status = SubscriptionStatus.Cancelled`'dır.
- **Token parametresi:** tekil kaydı adlandıran istek alanı her yerde `Token`'dır (`RefundPayment`, `CancelPayment`, `RetrievePayment`, `RetrieveOrder`, `UpdateOrder` …); `TransactionToken`, `OrderToken`, `SubscriptionToken`, `SavedCardToken` istek adları kalktı (ödemedeki `SavedCardToken` alanı yerinde).
- **Ödeme yanıtı iç içe:** `payment.TransactionToken` → `payment.Transaction.Token`, `payment.Status` → `payment.Transaction.Status`; iade ve iptalde `GiveBack.Type` / `Amount` → `GiveBack.Refund.Type` / `Amount`.
- **Enum'lar:** para birimi, dönem, durumlar, kart şeması ve tipi `string` değil `Odemehub.Enums` enum'larıdır; bilinmeyen değer `Unknown` okunur.
- **Müşteri:** `Customer { Reference, BillingAddress, ShippingAddress }` ve `Address`; eski düz müşteri alanları, `TaxDetails` ve istek tarafındaki `NamedCustomer` kalktı. Sipariş ve abonelik yanıtlarında müşteri hem üst seviyede hem kaydın üzerinde durur.
- **Kalemler:** katalog yok; `Item` adı, fiyatı, adedi ve KDV oranını kendisi taşır. `OrderItem` / `SubscriptionItem` yerine `Item`, gönderim için `ShippingMethod`.
- **Yanıt sınıfları:** `OrderDetails`, `OrderList`, `SubscriptionDetails`, `SubscriptionList`, `PaymentLinkDetails`, `PaymentLinkList`, `PaymentList`, `SavedCardDetails`, `SavedCardList`, `DeletedSavedCard`.
- **Hatalar:** güncellenen, silinen, iade ya da iptal edilen kaydın token'ı bulunamazsa `NotFoundException` (404) atılır; eskiden bu durum 422 dönüyordu. Sorgular bulunamayan kayıtta boş liste döner. Yeni istisnalar: `ForbiddenException` (403), `NotFoundException` (404), `RateLimitException` (429).
- **İstemci tarafı denetim yok:** kartla kayıtlı kartın birlikte verilmesi artık `ArgumentException` değil, geçitten 422'dir.
- **Webhook:** `OrderWebhook()`, `SubscriptionWebhook()`, `TransactionWebhook()` yerine tek `Webhook(method, path, body, timestamp, signature)` (ve `VerifyWebhook()`); imza istek ve yanıtlarla aynı şemadadır. Gövde yalnızca token taşır (`OrderToken`, `PaymentLinkToken`, `SubscriptionToken`, `TransactionToken`); durum `Retrieve*Async()` ile sorulur. Adresler panelde tanımlandığı için `SecurePayment`, `CreateOrder`, `UpdateOrder`, `CreateSubscription`, `UpdateSubscription` artık `WebhookUrl` almaz. `Signature`'ın yalnız gövdeyi imzalayan `Sign(body)` / `Verify(body, signature)` metotları kalktı.
- **Kanal kalktı.** `Options.ChannelToken`, isteklerdeki `ChannelToken` ve `ChannelMessage` yoktur. Referans alanları `ChannelReference` yerine `Reference` adını taşır (ödeme, sipariş, abonelik, link, kalem); yanıtlarda `ChannelToken` yoktur. Geri dönüşte tarayıcı `channel_reference` değil `reference` POST eder.
- **Sorgular tek uçta.** Her kaynakta tek sorgu metodu vardır: `RetrievePaymentsAsync`, `RetrieveOrdersAsync`, `RetrieveSubscriptionsAsync`, `RetrievePaymentLinksAsync`, `RetrieveSavedCardsAsync`. İstek `Token`, `Reference`, `CreatedFrom`/`CreatedTo` alır ya da boş verilir; yanıt her zaman listedir, bulunamayan kayıt `NotFoundException` değil boş listedir. GET isteği kalmadı.
- **Gönderim:** sipariş ve abonelik `RequiresShipping` ile ödeme sayfasında gönderim adresi ister; gönderim yöntemleri panelde tanımlanır, istekte gönderilmez. Yanıtta yalnızca ödeyenin seçtiği yöntem (`ShippingMethod`: `Reference`, `Title`, `Amount`, `TaxRate`) gelir.
- **Kalemler:** `TaxRate` isteğe bağlı; yeni `SaveAsProduct`.
- **Müşteri:** referans gönderilmeyebilir; o zaman müşteri kaydedilmez ve kart saklanamaz. Abonelikte ve kart saklamada zorunludur. `NamedCustomer.IsGuest` kalktı; `Reference` ve `BillingAddress` `null` olabilir.
- **Ödeme linki:** son 50 deneme ve `TransactionsCount` `RetrievePaymentLinksAsync` yanıtında her `PaymentLink` üzerindedir; `PaymentLinkDetails` yalnızca linki taşır.
- **Kayıtlı kart:** listede her kart kendi `Customer`'ını taşır; `SavedCardList.Customer` kalktı. Listelenen ödemede `SavedCard`, kartın saklanması istendiyse saklanan kartı verir.

## İmzayı elle doğrulamak

SDK imzayı `Odemehub.Signature` sınıfıyla üretir ve doğrular:

```csharp
var signature = new Signature(apiSecret);

signature.Sign("POST", "/api/1000000001/gateway/regular-payment", body, timestamp);
signature.Verify(method, path, body, request.Headers["X-Timestamp"], request.Headers["X-Signature"]);   // 5 dakikalık pencereyle
```

Test vektörü: `secret_test` anahtarıyla, `1700000000` anında, `/api/1000000001/gateway/regular-payment` yoluna `POST` edilen `{"a":1}` gövdesinin imzası `4d6225c9dd46837418b40dd8140d76a24cd7520d81ff3b280bf98da8da6a8771`'dir.
