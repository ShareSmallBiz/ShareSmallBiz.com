namespace HttpClientUtility.MemoryCache;

/// <summary>
/// Interface for memory cache operations
/// </summary>
public interface IMemoryCacheManager
{
    /// <summary>
    /// Clear all cache data
    /// </summary>
    void Clear();

    /// <summary>
    /// Dispose cache manager
    /// </summary>
    void Dispose();

    /// <summary>
    /// Get a cached item. If it's not in the cache yet, then load and cache it
    /// </summary>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="acquire">Function to load item if it's not in the cache yet</param>
    /// <param name="cacheTime">Cache time in minutes; pass 0 to not cache; pass null to use the default time</param>
    /// <returns>The cached value associated with the specified key</returns>
    T Get<T>(string key, Func<T> acquire, int? cacheTime = null);

    /// <summary>
    /// Get all keys stored in the cache
    /// </summary>
    /// <returns>List of all keys in the cache</returns>
    IList<string> GetKeys();

    /// <summary>
    /// Gets a value indicating whether the value associated with the specified key is cached
    /// </summary>
    /// <param name="key">Key of cached item</param>
    /// <returns>True if item already is in cache; otherwise false</returns>
    bool IsSet(string key);

    /// <summary>
    /// Perform some action with exclusive in-memory lock
    /// </summary>
    /// <param name="key">The key we are locking on</param>
    /// <param name="expirationTime">The time after which the lock will automatically expire</param>
    /// <param name="action">Action to be performed with locking</param>
    /// <returns>True if lock was acquired and action was performed; otherwise false</returns>
    bool PerformActionWithLock(string key, TimeSpan expirationTime, Action action);

    /// <summary>
    /// Removes the value with the specified key from the cache
    /// </summary>
    /// <param name="key">Key of cached item</param>
    void Remove(string key);

    /// <summary>
    /// Adds the specified key and object to the cache
    /// </summary>
    /// <param name="key">Key of cached item</param>
    /// <param name="data">Value for caching</param>
    /// <param name="cacheTimeMinutes">Cache time in minutes</param>
    void Set(string key, object data, int cacheTimeMinutes);
}
