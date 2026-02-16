namespace Elm.Application.Contracts.Abstractions.Cache
{
    public interface IGenericCacheService
    {
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        void Remove(string key);
    }
}
