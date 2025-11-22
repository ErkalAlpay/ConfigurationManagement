using Configuration.Core.Repository;
using StackExchange.Redis;

namespace Configuration.Core.Factory
{
    public class RedisConfigurationRepositoryFactory : IConfigurationRepositoryFactory
    {
        public IConfigurationRepository CreateRepository(string connectionString)
        {
            // Connection string formatları: 
            // "redis://host:port" veya "host:port" veya sadece "host:port"
            string normalizedConnectionString = NormalizeConnectionString(connectionString);
            
            var redis = ConnectionMultiplexer.Connect(normalizedConnectionString);
            return new RedisConfigurationRepository(redis);
        }

        private string NormalizeConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

            // Eğer "redis://" ile başlamıyorsa ve ":" içeriyorsa, direkt kullan
            // Redis connection string formatı: "host:port" veya "host:port,options"
            if (!connectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) 
                && !connectionString.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
                && connectionString.Contains(":"))
            {
                // Zaten doğru formatta
                return connectionString;
            }

            // "redis://" prefix'ini kaldır
            if (connectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase))
            {
                connectionString = connectionString.Substring(8);
            }

            return connectionString;
        }
    }
}

