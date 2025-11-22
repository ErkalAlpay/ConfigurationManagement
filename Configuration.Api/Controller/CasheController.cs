using System.Text.Json;
using Configuration.Core.Entity;
using Configuration.Core.Entity.Dto;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly ICacheService _cacheService;

    public CacheController(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetValue(string key, [FromHeader(Name = "Application-Name")] string appName)
    {
        var redisKey = $"{appName}:{key}";
        var json = await _cacheService.GetRawAsync(redisKey);

        if (json == null)
            return NotFound(new { message = "Key not found" });

        //JSON → ENTITY
        var entity = JsonSerializer.Deserialize<ConfigurationRecord>(json);

        if (entity == null)
            return BadRequest("Invalid config format.");

        if (entity.ApplicationName != appName)
            return Forbid();

        //Sadece aktif kayıtlar dönmeli
        if (entity.IsActive != 1)
            return NotFound(new { message = "Configuration is not active" });

        //ENTITY → DTO
        var dto = new ConfigurationRecordDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            Value = entity.Value,
            IsActive = entity.IsActive == 1,
            ApplicationName = entity.ApplicationName
        };

        return Ok(dto);
    }
}
