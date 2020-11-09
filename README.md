[![Nuget](https://img.shields.io/nuget/v/AO.DbCache)](https://www.nuget.org/packages/AO.DbCache/)

This came from a need to throttle API calls to a 3rd party service. I wanted a simple way to cache results from previous calls with a dictionary key, and set a time limit after which the cache expires. After that, the live data source can be queried again. If I call a service within a specified time span, then I get the cached data. And, do all this with minimal impact to my existing code.

This uses the [DbDictionary](https://github.com/adamfoneil/Dapper.CX/blob/master/Dapper.CX.Base/Abstract/DbDictionary.cs) class from my Dapper.CX project. This project builds upon this with the [DbCache](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs) class. `DbCache` is an abstract class so you must provide your own json serialization. See the integration [tests](https://github.com/adamfoneil/DbCache/blob/master/Testing/CacheTests.cs) to see a sample use along with the [SampleDbCache](https://github.com/adamfoneil/DbCache/blob/master/Testing/SampleDbCache.cs) implementation. The tests use my [CloudObjects](https://cloudobjects.azurewebsites.net/) service. Here's the essence of it. Let's say you have this API call:

```csharp
var cloudObj = await client.GetAsync<SampleObject>(objectName);
return cloudObj.Object;
```
Let's say you're okay fetching this at most every hour. You would use `DbCache` to store the call result with a string key. This example says: get `SampleObject` with key `object1` a maximum of once per hour. If less than an hour has passed since the last call, then the cached data is returned. Otherwise, the live data is returned.

```csharp
var fetched = await cache.GetAsync("object1",
    async () =>
    {
        var cloudObj = await client.GetAsync<SampleObject>(objectName);
        return cloudObj.Object;
    }, TimeSpan.FromMinutes(60));
```

To determine if the returned data came from the cache or live source, use the [Source](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs#L25) property.
