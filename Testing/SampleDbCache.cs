using DbCacheLibrary;
using System;
using System.Data;
using System.Text.Json;

namespace Testing
{
    public class SampleDbCache : DbCache
    {
        public SampleDbCache(Func<IDbConnection> getConnection) : base(getConnection, "dbo.CloudObjectsCache")
        {
        }

        protected override TValue Deserialize<TValue>(string value) => JsonSerializer.Deserialize<TValue>(value);

        protected override string Serialize<TValue>(TValue value) => JsonSerializer.Serialize(value);
    }
}
