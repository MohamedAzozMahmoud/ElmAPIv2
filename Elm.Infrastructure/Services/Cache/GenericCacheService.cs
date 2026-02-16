using Elm.Application.Contracts.Abstractions.Cache;
using Microsoft.Extensions.Caching.Memory;

namespace Elm.Infrastructure.Services.Cache
{
    public class GenericCacheService : IGenericCacheService
    {
        private readonly IMemoryCache _memoryCache;

        public GenericCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (_memoryCache.TryGetValue(key, out T value))
            {
                return value;
            }
            value = await factory();
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(10)
            };
            _memoryCache.Set(key, value, cacheEntryOptions);
            return value;
        }

        public void Remove(string key)
        {
            _memoryCache.Remove(key);
        }
    }
}
