public interface ICacheService
{
    Task<string?> GetRawAsync(string key);
    Task SetRawAsync(string key, string json, int minutes = 30);
    Task RemoveAsync(string key);
}