using Dapper.CX.Abstract;
using System;
using System.Data;
using System.Threading.Tasks;

namespace DbCacheLibrary
{
    public abstract class DbCache : DbDictionary<string>
    {
        public DbCache(Func<IDbConnection> getConnection, string tableName) : base(getConnection, tableName)
        {
        }

        /// <summary>
        /// string to prepend before every key (for example by user name)
        /// </summary>
        public string KeyPrefix { get; set; }

        public async Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> accessor, TimeSpan maxAge)
        {
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
            }
            else
            {
                // or just give me the cached data
                value = Deserialize<TValue>(entry.Value);
            }

            return value;            
        }       

        public new async Task SetAsync<TValue>(string key, TValue value)
        {
            key = KeyPrefix + key;
            await base.SetAsync(key, value);
        }
    }
}
