using Dapper.CX.Abstract;
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

        public async Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> accessor, TimeSpan maxAge)
        {
            await InitializeAsync();            

            key = KeyPrefix + key;
            TValue value;

            var entry = await GetRowAsync(key);
            
            var age = (entry != null) ? 
                DateTime.UtcNow.Subtract(entry.DateModified ?? entry.DateCreated) : 
                TimeSpan.MaxValue;

            // if the cache data is expired
            if (age > maxAge)
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
    }
}
