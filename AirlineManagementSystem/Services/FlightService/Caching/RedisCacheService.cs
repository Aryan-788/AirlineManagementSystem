using StackExchange.Redis;
using System.Text.Json;

namespace FlightService.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<bool> RemoveAsync(string key);
    Task<bool> AcquireLockAsync(string key, TimeSpan ttl);
    Task<bool> ReleaseLockAsync(string key);
}

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Get a cached value by key. Returns default(T) if not found or Redis is unavailable.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            
            if (!value.HasValue)
                return default;

            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Redis Get failed for key '{key}': {ex.Message}. Falling back to null.");
            return default;
        }
    }

    /// <summary>
    /// Set a cached value with optional expiry. Returns true if successful, false otherwise.
    /// </summary>
    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var db = _redis.GetDatabase();
            var serialized = JsonSerializer.Serialize(value);
            return await db.StringSetAsync(key, serialized, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Redis Set failed for key '{key}': {ex.Message}. Data will not be cached.");
            return false;
        }
    }

    /// <summary>
    /// Remove a cached value by key. Returns true if removed, false if key didn't exist or Redis is unavailable.
    /// </summary>
    public async Task<bool> RemoveAsync(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            return await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Redis Remove failed for key '{key}': {ex.Message}.");
            return false;
        }
    }

    /// <summary>
    /// Acquire a distributed lock using Redis. Uses SET with NX option for atomicity.
    /// Returns true if lock acquired, false if already locked or Redis is unavailable.
    /// </summary>
    public async Task<bool> AcquireLockAsync(string key, TimeSpan ttl)
    {
        try
        {
            var db = _redis.GetDatabase();
            var lockValue = Guid.NewGuid().ToString();
            // Use SET with NX (only if not exists) and EX (expiry) for atomic lock acquisition
            var acquired = await db.StringSetAsync(key, lockValue, ttl, When.NotExists);
            
            if (acquired)
                _logger.LogDebug($"Lock acquired for key '{key}'.");
            else
                _logger.LogDebug($"Lock already held for key '{key}'.");

            return acquired;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Redis AcquireLock failed for key '{key}': {ex.Message}. Lock will not be acquired.");
            return false;
        }
    }

    /// <summary>
    /// Release a distributed lock. Returns true if released, false if key didn't exist or Redis is unavailable.
    /// </summary>
    public async Task<bool> ReleaseLockAsync(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            var deleted = await db.KeyDeleteAsync(key);
            
            if (deleted)
                _logger.LogDebug($"Lock released for key '{key}'.");
            else
                _logger.LogDebug($"Lock not found for key '{key}'. May have already expired.");

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Redis ReleaseLock failed for key '{key}': {ex.Message}. Lock may remain active.");
            return false;
        }
    }
}
