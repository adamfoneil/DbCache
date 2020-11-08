[![Nuget](https://img.shields.io/nuget/v/AO.DbCache)](https://www.nuget.org/packages/AO.DbCache/)

This came from a need to throttle API calls to a 3rd party service. I wanted a simple way to cache results from previous calls with a dictionary key, and set a time limit after which the cache expires. After that, the live data source is queried again. If I call a service within a specified time span, then I get the cached data. It works with any type that can be json serialized.

This uses the [DbDictionary](https://github.com/adamfoneil/Dapper.CX/blob/master/Dapper.CX.Base/Abstract/DbDictionary.cs) class from my Dapper.CX project. This project builds upon this with the [DbCache](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs) class. `DbCache` is an abstract class so you can provide your own json serialization.

See the integration [tests](https://github.com/adamfoneil/DbCache/blob/master/Testing/CacheTests.cs) to see a sample use. The tests use my [CloudObjects](https://cloudobjects.azurewebsites.net/) service. Here's the essence of it. This example says: get `SampleObject` with key `object1` a maximum of once every 30 seconds. If more than 30 seconds has passed since the last fetch, it's okay to query it live. Otherwise, use the cached version.

```csharp
var fetched = await cache.GetAsync("object1",
    async () =>
    {
        var cloudObj = await client.GetAsync<SampleObject>(objectName);
        return cloudObj.Object;
    }, TimeSpan.FromSeconds(30));
```
