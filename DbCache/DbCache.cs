using Dapper.CX.Abstract;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Data;
using System.Threading.Tasks;

namespace DbCacheLibrary
{
    public enum ObjectSource
    {
        Cache,
        Live
    }

    public abstract class DbCache : DbDictionary<string>
    {
        public DbCache(Func<IDbConnection> getConnection, string tableName) : base(getConnection, tableName)
        {
        }

        /// <summary>
        /// string to prepend before every key (for example by user name)
        /// </summary>
        public string KeyPrefix { get; set; }

        public ObjectSource Source { get; private set; }

        private async Task<TValue> GetInnerAsync<TValue>(string key, Func<Task<TValue>> accessor, Func<DictionaryRow, bool> expirationCheck)
        {
            await InitializeAsync();

            key = KeyPrefix + key;
            TValue value;

            var entry = await GetRowAsync(key);

            if (expirationCheck.Invoke(entry))
            {
                // query it anew and store
                value = await accessor.Invoke();
                await SetAsync(key, value);
                Source = ObjectSource.Live;
            }
            else
            {
                // or just give me the cached data
                value = Deserialize<TValue>(entry.Value);
                Source = ObjectSource.Cache;
            }

            return value;
        }

        public async Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> accessor, TimeSpan maxAge) =>
            await GetInnerAsync(key, accessor, (entry) => HasElapsed(entry, maxAge));

        public async Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> accessor, DateTime expireAfter) =>
            await GetInnerAsync(key, accessor, (entry) => HasExpired(entry, expireAfter));

        private bool HasExpired(DictionaryRow entry, DateTime expireAfter)
        {
            var entryDate = (entry != null) ?
                entry.DateModified ?? entry.DateCreated :
                DateTime.MinValue;

            return (expireAfter > entryDate);
        }

        private bool HasElapsed(DictionaryRow entry, TimeSpan maxAge)
        {
            var age = (entry != null) ?
                DateTime.UtcNow.Subtract(entry.DateModified ?? entry.DateCreated) :
                TimeSpan.MaxValue;

            return (age > maxAge);
        }
    }
}
