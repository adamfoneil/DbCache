using CloudObjects.Client;
using CloudObjects.Client.Models;
using CloudObjects.Client.Static;
using DbCacheLibrary;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlServer.LocalDb;
using System;
using System.Threading.Tasks;

namespace Testing
{
    [TestClass]
    public class CacheTests
    {
        [TestMethod]
        public void CloudObjectsLiveSource()
        {
            var config = GetConfig();
            var creds = new ApiCredentials();
            config.Bind("CloudObjects", creds);
            var client = new CloudObjectsClient(HostLocations.Online, creds);

            const string objectName = "object1";
            var local = new SampleObject()
            {
                FirstName = "jinga",
                LastName = "zamooga"
            };

            client.SaveAsync(objectName, local).Wait();

            Task.Delay(3000);

            var cache = new SampleDbCache(() => LocalDb.GetConnection("DbCacheTest"));
            var fetched = cache.GetAsync(objectName, 
                async () =>
                {
                    var cloudObj = await client.GetAsync<SampleObject>(objectName);
                    return cloudObj.Object;
                }, TimeSpan.FromSeconds(2)).Result;

            Assert.IsTrue(fetched.FirstName.Equals(local.FirstName));
            Assert.IsTrue(fetched.LastName.Equals(local.LastName));
            Assert.IsTrue(cache.Source == ObjectSource.Live);
        }

        private IConfiguration GetConfig() => new ConfigurationBuilder().AddJsonFile("Config/config.json").Build();
    }

    internal class SampleObject
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

}
