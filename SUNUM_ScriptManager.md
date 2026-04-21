# Script Manager — Sunum Taslağı (metin + pptx: kapak + içerik + kapanış)

Aşağıdaki her `# Sayfa` bloğu bir slayt olarak düşünülebilir. İstersen başlığı slayt başlığı, madde işaretlerini gövde metni yap.

---

## Sayfa 1 — Proje nedir?

**Script Manager**

- Veritabanına gidecek **T-SQL script’lerini** tek sistemde toplayan **kurumsal web uygulaması**.
- Script’ler **release (sürüm)** ve **batch (iş paketi)** hiyerarşisi altında organize edilir.
- **Geliştirici** ve **testçi** rolleriyle **durum akışı** (taslak → inceleme → hazır, çakışma vb.) desteklenir.
- **İki erişim kanalı:**
  - **Web (MVC):** İnsanların kullandığı arayüz, tarayıcı oturumu.
  - **API:** Entegrasyon ve otomasyon için REST uçları, JWT ile kimlik.

**Tek cümle:** Veritabanı değişikliklerini dağınık dosya/mail yerine **süreçli, izlenebilir ve kontrollü** şekilde yönetmek için geliştirilmiştir.

---

## Sayfa 2 — İş süreçleri: Release, batch, script, çakışma

- **Release oluşturma:** Sürüm adı, versiyon, açıklama; batch ağacının kökü; **iptal** ve **ağaç kilidi** (release ağacında değişikliği kısıtlama).
- **Batch oluşturma:** Hiyerarşik iş paketleri (klasör/alt batch); release’e bağlama veya bağımsız çalışma; **kilitli batch**’e yeni script **eklenemez** (iş kuralı).
- **Script:** Oluşturma/güncelleme; **T-SQL doğrulama**; durum akışı: **taslak** → testçi incelemesi → **hazır**; gerekirse **Conflict** durumu.
- **Script çakışması:** Script kaydedilince **ScriptConflictSyncService**, aynı release kapsamındaki (veya batch ağacındaki) diğer script’lerle karşılaştırır; SQL’den **ConflictKey** çıkarılır; eşleşen çiftler **Conflict** kaydına düşer; çözüm veya görmezden gelmede **SHA256 fingerprint** ile SQL değişince kayıtların yeniden değerlendirilmesi.

---

## Sayfa 3 — Amaçlar (iş değeri)

| Amaç | Nasıl karşılanıyor? |
|------|---------------------|
| **Tek doğru kaynak** | Script’ler release/batch altında merkezi veritabanında |
| **Hata riskini azaltma** | Kayıt öncesi **T-SQL sözdizimi doğrulaması** (ScriptDom, isteğe bağlı sunucu parse) |
| **Çakışma farkındalığı** | Aynı kapsamdaki script’lerde **nesne/tablo/kayıt** düzeyinde çakışma tespiti ve kayıt |
| **İş akışı** | Rol bazlı yetkiler; script yaşam döngüsü durumları |
| **Güvenli operasyon** | Kilitli batch’e ekleme engeli; rollback alanı; soft delete; parola hash |

**Özet:** Üretim öncesi **kalite ve koordinasyon**; ekiplerin aynı veritabanı üzerinde **bilinçli** şekilde çalışması.

---

## Sayfa 4 — Teknolojiler ve nedenleri

| Teknoloji | Neden seçildi? |
|-----------|------------------|
| **.NET 9** | Güncel platform, tooling ve performans |
| **ASP.NET Core MVC** | Form ve sayfa tabanlı kurumsal UI, cookie oturumu |
| **ASP.NET Core Web API** | Dış sistemlerin script işlemlerini programatik yapması |
| **Entity Framework Core + SQL Server** | İlişkisel model, **migration** ile şema sürümü, güçlü sorgu desteği |
| **MediatR** | İstek → handler; **ince controller**; BLL’de tek iş kuralı; API ve MVC paylaşımı |
| **Cookie auth (MVC)** | Tarayıcı kullanıcısı için doğal oturum modeli |
| **JWT Bearer (API)** | Stateless API istemcileri için endüstri standardı |
| **Swagger** | API sözleşmesi ve hızlı test |
| **Transact-SQL ScriptDom** | Microsoft’un T-SQL parser’ı; güvenilir sözdizimi ve AST |
| **Microsoft.Data.SqlClient** | İsteğe bağlı **SET NOEXEC** ile motor tarafı doğrulama |
| **SHA256 (fingerprint)** | SQL değişince “kullanıcı çakışmayı kabul etti” kaydının geçersiz kalması |

**Prensip:** Doğru araç, doğru katmanda — UI/API ince, kural **BLL**’de, veri **DAL**’da.

---

## Sayfa 5 — Çok katmanlı mimari: katmanlar ne işe yarar?

**Neden çok katmanlı?**  
Sorumlulukları ayırarak **bakımı kolaylaştırmak**, aynı iş kurallarını **web ve API** ile paylaşmak, değişiklikleri **sınırlı alanda** tutmak.

| Katman | Görev | Bu projede |
|--------|--------|------------|
| **Sunum** | Kullanıcı/API ile etkileşim | **MVC** (HTML, cookie), **API** (JSON, JWT) |
| **İş mantığı (BLL)** | Kurallar, validasyon, çakışma, use case’ler | **MediatR** handler’lar, servisler (syntax validator, conflict sync) |
| **Veri erişimi (DAL)** | Kalıcılık, şema, sorgu soyutlama | **EF Core**, entity’ler, **repository**’ler, **migration**’lar |

**Akış (basit):**  
İstek → Controller → **`Mediator.Send(Request)`** → Handler (BLL) → Repository / DbContext (DAL) → SQL Server.

**CQRS’e yakın düzen:** BLL’de **`Commands`** (yazma) ve **`Queries`** (okuma) ayrımı — okunabilirlik ve genişleme kolaylığı.

---

## Sayfa 6 — Kapanış: özet mesaj

- **Script Manager:** SQL Server’a gidecek değişiklikleri **yöneten**, **doğrulayan** ve **çakışmaları görünür kılan** kurumsal bir araçtır.
- **Mimari:** Katmanlı yapı + MediatR ile **tek çekirdek iş kuralı**, **çift sunum** (MVC + API).
- **Teknoloji:** .NET 9, EF Core, ScriptDom/SqlClient, cookie + JWT, Swagger.
- **Değer:** Daha az sürpriz, daha izlenebilir veritabanı değişiklik süreci.

**Olası soru:** “Neden hem MVC hem API?” — İnsan işi **web**; otomasyon **API**; ikisi de aynı **BLL/DAL**’ı kullanır.

---

*Bu dosya sunum taslağıdır; slaytlara aktarırken görseller (basit mimari şema, ekran görüntüsü) eklenebilir.*
