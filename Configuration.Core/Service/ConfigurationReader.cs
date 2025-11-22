using Configuration.Core.Entity;
using Configuration.Core.Repository;
using Configuration.Core.Factory;

namespace Configuration.Core.Service
{
    public class ConfigurationReader : IDisposable
    {
        private readonly string _applicationName;
        private readonly IConfigurationRepository _repository;
        private readonly int _refreshIntervalMs;
        private readonly Timer _refreshTimer;
        private readonly object _cacheLock = new object();

        private Dictionary<string, ConfigurationRecord> _cache = new();
        
        //Son başarılı cache (storage erişilemediğinde kullanılacak)
        private Dictionary<string, ConfigurationRecord> _lastSuccessfulCache = new();

        public ConfigurationReader(string applicationName, string connectionString, int refreshTimerIntervalInMs)
        {
            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentException("Application name cannot be null or empty", nameof(applicationName));
            
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

            _applicationName = applicationName;
            _refreshIntervalMs = refreshTimerIntervalInMs;

            IConfigurationRepositoryFactory factory = CreateFactory(connectionString);
            _repository = factory.CreateRepository(connectionString);

            //İlk yükleme - başarısız olursa boş cache ile devam et
            try
            {
                RefreshCache(null);
            }
            catch
            {

            }

            _refreshTimer = new Timer(RefreshCache, null, _refreshIntervalMs, _refreshIntervalMs);
        }

        private IConfigurationRepositoryFactory CreateFactory(string connectionString)
        {
            if (connectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) 
                || connectionString.Contains(":") && !connectionString.Contains(";"))
            {
                return new RedisConfigurationRepositoryFactory();
            }
            
            return new RedisConfigurationRepositoryFactory();
        }

        //Timer callback
        private void RefreshCache(object? state)
        {
            try
            {
                // Repository üzerinden aktif kayıtları al
                var records = _repository.GetAllActiveByApplication(_applicationName);
                var newCache = records.ToDictionary(r => r.Name, r => r);

                //Başarılı refresh - cache'leri güncelle
                lock (_cacheLock)
                {
                    _cache = newCache;
                    _lastSuccessfulCache = new Dictionary<string, ConfigurationRecord>(newCache);
                }

                Console.WriteLine($"Cache refresh başarılı: {DateTime.Now} - {newCache.Count} kayıt yüklendi");
            }
            catch (Exception ex)
            {
                //Storage erişilemediğinde son başarılı cache'i kullan
                lock (_cacheLock)
                {
                    if (_lastSuccessfulCache.Count > 0)
                    {
                        _cache = new Dictionary<string, ConfigurationRecord>(_lastSuccessfulCache);
                        Console.WriteLine($"Storage erişilemedi, son başarılı cache kullanılıyor: {DateTime.Now} - Hata: {ex.Message}");
                    }
                    else
                    {
                        // İlk yüklemede hata ve cache yoksa boş cache ile devam et
                        Console.WriteLine($"Storage erişilemedi ve cache yok: {DateTime.Now} - Hata: {ex.Message}");
                    }
                }
            }
        }
        public T GetValue<T>(string key)
        {
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(key, out var record))
                {
                    try
                    {
                        return ConvertValue<T>(record);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidCastException($"Key '{key}' cannot be converted to type {typeof(T).Name}. Error: {ex.Message}");
                    }
                }
                throw new KeyNotFoundException($"Key '{key}' not found in configuration for {_applicationName}");
            }
        }

        private T ConvertValue<T>(ConfigurationRecord record)
        {
            var targetType = typeof(T);
            var value = record.Value;
            var recordType = record.Type?.ToLowerInvariant() ?? "string";

            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            switch (recordType)
            {
                case "bool":
                case "boolean":
                    if (underlyingType == typeof(bool))
                    {
                        // "1", "0", "true", "false" değerlerini destekle
                        if (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase))
                            return (T)(object)true;
                        if (value == "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase))
                            return (T)(object)false;
                        throw new InvalidCastException($"Cannot convert '{value}' to boolean");
                    }
                    break;

                case "int":
                case "integer":
                    if (underlyingType == typeof(int))
                    {
                        if (int.TryParse(value, out var intValue))
                            return (T)(object)intValue;
                        throw new InvalidCastException($"Cannot convert '{value}' to integer");
                    }
                    break;

                case "double":
                    if (underlyingType == typeof(double))
                    {
                        if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var doubleValue))
                            return (T)(object)doubleValue;
                        throw new InvalidCastException($"Cannot convert '{value}' to double");
                    }
                    break;

                case "string":
                    if (underlyingType == typeof(string))
                    {
                        return (T)(object)value;
                    }
                    break;
            }

            //Eğer record type ile uyumsuzsa, direkt tip dönüşümü dene
            try
            {
                return (T)Convert.ChangeType(value, underlyingType, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                throw new InvalidCastException($"Cannot convert value '{value}' (Type: {recordType}) to {targetType.Name}");
            }
        }

        public void Dispose()
        {
            _refreshTimer?.Dispose();
        }
    }
}
