# Configuration Management System

## Genel Bakış

Dinamik konfigürasyon yönetim sistemi. Statik dosyalar (appsettings.json, web.config) yerine Redis kullanır. Restart gerektirmeden güncelleme yapılabilir.

## Özellikler

- ✅ Dinamik konfigürasyon (restart gerektirmez)
- ✅ Merkezi yönetim (Redis)
- ✅ Uygulama izolasyonu (ApplicationName bazlı)
- ✅ Otomatik refresh (periyodik güncelleme)
- ✅ Hata toleransı (storage erişilemediğinde son cache kullanılır)

## Kurulum

### Docker ile
```bash
docker-compose up --build
```

### Manuel
```bash
# Redis başlat
docker run -d -p 6379:6379 redis:7

# API çalıştır
cd Configuration.Api
dotnet run

# Web UI çalıştır
cd Configuration.Web
dotnet run
```

## Kullanım

### ConfigurationReader Kütüphanesi

```csharp
using Configuration.Core.Service;

// Initialize
var config = new ConfigurationReader(
    applicationName: "SERVICE-A",
    connectionString: "localhost:6379",
    refreshTimerIntervalInMs: 40000
);

// Kullanım
string siteName = config.GetValue<string>("SiteName");
bool isEnabled = config.GetValue<bool>("IsBasketEnabled");
int maxCount = config.GetValue<int>("MaxItemCount");

// Dispose
config.Dispose();
```

### Dependency Injection (ASP.NET Core)

```csharp
builder.Services.AddSingleton<ConfigurationReader>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis");
    return new ConfigurationReader(
        "SERVICE-A",
        connectionString ?? "localhost:6379",
        40000
    );
});
```

## API Endpoints

Base URL: `http://localhost:8080`

**Tüm endpoint'ler `Application-Name` header gerektirir.**

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| GET | `/api/configuration` | Tüm konfigürasyonları listele |
| GET | `/api/configuration/{key}` | Tek bir konfigürasyon getir |
| POST | `/api/configuration` | Yeni konfigürasyon ekle |
| PUT | `/api/configuration/{key}` | Konfigürasyon güncelle |
| DELETE | `/api/configuration/{key}` | Konfigürasyon sil |

**Örnek Request:**
```http
POST /api/configuration
Headers:
  Application-Name: SERVICE-A
  Content-Type: application/json
Body:
{
  "name": "SiteName",
  "type": "string",
  "value": "soty.io",
  "isActive": true
}
```

**Swagger UI:** `http://localhost:8080/swagger`

## Web Arayüzü

- URL: `http://localhost:5000` (veya Web projesinin portu)
- Özellikler: Listeleme, Ekleme, Düzenleme, Silme, Filtreleme

## Tip Dönüşümleri

| Type | Desteklenen Değerler |
|------|---------------------|
| `string` | Herhangi bir string |
| `bool` | `"1"`, `"0"`, `"true"`, `"false"` |
| `int` | Sayısal değerler |
| `double` | Ondalıklı sayılar |

## Yapılandırma

### appsettings.json
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

### Docker Compose
```yaml
services:
  configuration-api:
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__Redis: "redis:6379"
  redis:
    image: redis:7
    ports:
      - "6379:6379"
```

## Önemli Notlar

- Her uygulama sadece kendi `ApplicationName`'ine ait kayıtları görür
- `ConfigurationReader` otomatik olarak periyodik güncelleme yapar
- Storage erişilemediğinde son başarılı cache kullanılır
- Thread-safe çalışır
- Kullanım sonrası `Dispose()` çağrılmalıdır

## Hata Yönetimi

- `KeyNotFoundException`: Key bulunamadı
- `InvalidCastException`: Tip dönüşümü başarısız
- Storage erişim hatası: Son başarılı cache kullanılır

