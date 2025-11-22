using System.Text.Json;
using System.Linq;
using Configuration.Core.Entity;
using Configuration.Core.Entity.Dto;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace Configuration.Api.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IDatabase _redisDb;

        public ConfigurationController(IConnectionMultiplexer redis)
        {
            _redisDb = redis.GetDatabase();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromHeader(Name = "Application-Name")] string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
                return BadRequest(new { message = "Application-Name header is required" });

            var server = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{appName}:*").ToArray();

            var records = new List<ConfigurationRecordDto>();

            foreach (var key in keys)
            {
                var value = await _redisDb.StringGetAsync(key);
                if (value.IsNullOrEmpty) continue;

                var jsonString = value.ToString();
                if (string.IsNullOrEmpty(jsonString)) continue;

                var record = JsonSerializer.Deserialize<ConfigurationRecord>(jsonString);
                if (record == null || record.ApplicationName != appName) continue;

                records.Add(new ConfigurationRecordDto
                {
                    Id = record.Id,
                    Name = record.Name,
                    Type = record.Type,
                    Value = record.Value,
                    IsActive = record.IsActive == 1,
                    ApplicationName = record.ApplicationName
                });
            }

            return Ok(records);
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> GetValue(string key, [FromHeader(Name = "Application-Name")] string appName)
        {
            var redisKey = $"{appName}:{key}";
            var value = await _redisDb.StringGetAsync(redisKey);

            if (value.IsNullOrEmpty)
                return NotFound(new { message = $"Key '{key}' not found." });

            var jsonString = value.ToString();
            if (string.IsNullOrEmpty(jsonString))
                return NotFound(new { message = $"Key '{key}' not found." });

            var record = JsonSerializer.Deserialize<ConfigurationRecord>(jsonString);
            if (record == null)
                return BadRequest(new { message = "Invalid record format." });

            if (record.ApplicationName != appName)
                return StatusCode(403, new { message = "Access denied '{appName}'" });

            //ENTITY → DTO dönüşümü
            var dto = new ConfigurationRecordDto
            {
                Id = record.Id,
                Name = record.Name,
                Type = record.Type,
                Value = record.Value,
                IsActive = record.IsActive == 1,
                ApplicationName = record.ApplicationName
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> SetValue([FromHeader(Name = "Application-Name")] string appName, [FromBody] ConfigurationRecordRequest request)
        {
            var redisKey = $"{appName}:{request.Name}";
            
            //Mevcut kaydı kontrol et
            var existing = await _redisDb.StringGetAsync(redisKey);
            int id = 0;
            
            if (!existing.IsNullOrEmpty)
            {
                var existingRecord = JsonSerializer.Deserialize<ConfigurationRecord>(existing!);
                if (existingRecord != null)
                {
                    id = existingRecord.Id;
                }
            }
            else
            {
                //Yeni kayıt için Id oluştur - mevcut kayıtların sayısına göre
                var server = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints().First());
                var keys = server.Keys(pattern: $"{appName}:*").ToArray();
                id = keys.Length > 0 ? keys.Length + 1 : 1;
            }
            
            //request → ENTITY dönüşümü
            var entity = new ConfigurationRecord
            {
                Id = id,
                Name = request.Name,
                Type = request.Type,
                Value = request.Value,
                IsActive = request.IsActive ? 1 : 0,
                ApplicationName = appName
            };

            var json = JsonSerializer.Serialize(entity);
            await _redisDb.StringSetAsync(redisKey, json);
            return Ok(new
            {
                message = $"Key '{redisKey}' created successfully.",
                data = request
            });
        }

        [HttpPut("{key}")]
        public async Task<IActionResult> UpdateValue(string key, [FromHeader(Name = "Application-Name")] string appName, [FromBody] ConfigurationRecordUpdateRequest request)
        {
            var redisKey = $"{appName}:{key}";

            var existing = await _redisDb.StringGetAsync(redisKey);
            if (existing.IsNullOrEmpty)
                return NotFound(new { message = $"Key '{key}' not found for '{appName}'." });

            var existingJson = existing.ToString();
            if (string.IsNullOrEmpty(existingJson))
                return NotFound(new { message = $"Key '{key}' not found for '{appName}'." });

            var record = JsonSerializer.Deserialize<ConfigurationRecord>(existingJson);

            if (record == null)
                return BadRequest("Existing record could not be parsed.");

            record.Value = request.Value;
            record.IsActive = request.IsActive ? 1 : 0;

            var json = JsonSerializer.Serialize(record);
            await _redisDb.StringSetAsync(redisKey, json);

            return Ok(new { message = $"Key '{key}' updated successfully for '{appName}'." });
        }

        [HttpDelete("{key}")]
        public async Task<IActionResult> DeleteValue(string key, [FromHeader(Name = "Application-Name")] string appName)
        {
            var redisKey = $"{appName}:{key}";

            var removed = await _redisDb.KeyDeleteAsync(redisKey);

            if (!removed)
                return NotFound(new { message = $"Key '{key}' not found for application '{appName}'." });

            return Ok(new { message = $"Key '{key}' deleted successfully for '{appName}'." });
}
    }
}
