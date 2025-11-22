using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Configuration.Core.Entity;
using StackExchange.Redis;

namespace Configuration.Core.Repository
{
    public class RedisConfigurationRepository : IConfigurationRepository
    {
        private readonly IDatabase _db;
        private readonly IConnectionMultiplexer _redis;

        public RedisConfigurationRepository(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = redis.GetDatabase();
        }

        public IEnumerable<ConfigurationRecord> GetAllActiveByApplication(string applicationName)
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());

            var keys = server.Keys(pattern: $"{applicationName}:*").ToArray();

            foreach (var key in keys)
            {
                var json = _db.StringGet(key);
                if (json.IsNull) continue;

                var jsonString = json.ToString();
                if (string.IsNullOrEmpty(jsonString)) continue;

                var record = JsonSerializer.Deserialize<ConfigurationRecord>(jsonString);

                if (record != null && record.IsActive == 1)
                    yield return record;
            }
        }
    }
}

