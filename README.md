[![Nuget](https://img.shields.io/nuget/v/AO.DbCache)](https://www.nuget.org/packages/AO.DbCache/)

This came from a need to throttle API calls to a 3rd party service. I wanted a simple way to cache results from previous calls with a dictionary key in my SQL Server database, and set a time limit after which the cache expires. After that, the live data source can be queried again. If I call a service within a specified time span, then I get the cached data.

This uses the [DbDictionary](https://github.com/adamfoneil/Dapper.CX/blob/master/Dapper.CX.Base/Abstract/DbDictionary.cs) class from my Dapper.CX project. This project builds upon this with the [DbCache](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs) class. `DbCache` is an abstract class so you must provide your own json serialization. See the integration [tests](https://github.com/adamfoneil/DbCache/blob/master/Testing/CacheTests.cs) to see a sample use along with the [SampleDbCache](https://github.com/adamfoneil/DbCache/blob/master/Testing/SampleDbCache.cs) implementation. The tests use my [CloudObjects](https://cloudobjects.azurewebsites.net/) service. Here's the essence of it. Let's say you have this API call:

```csharp
var cloudObj = await client.GetAsync<SampleObject>(objectName);
return cloudObj.Object;
```
Let's say you're okay fetching live data from this at most every hour. You would use `DbCache` to store the call result with a string key. This example says: get `SampleObject` with key `object1` a maximum of once per hour. If less than an hour has passed since the last call, then the cached data is returned. Otherwise, the live data is returned, and the cache is updated again and used for another hour.

```csharp
var fetched = await cache.GetAsync("object1",
    async () =>
    {
        var cloudObj = await client.GetAsync<SampleObject>(objectName);
        return cloudObj.Object;
    }, TimeSpan.FromMinutes(60));
```

To determine if the returned data came from the cache or live source, use the [Source](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs#L26) property.

# Using SetEachAsync
If your API call returns a json array, you can use `SetEachAsync` to store individual array elements as separate cache rows. This lets you get these individual elements later without enumerating the original array again. There are two overloads, [one](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs#L67) that stores your original json data as-is, and another lets you [transform](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs#L81) or modify the json in some way. Both overloads return `Dictionary<string, int>` with all the keys and generated cache row `Id` values.

Data saved with `SetEachAsync` has no expiration timespan or date. Since `DbCache` derives from `DbDictionary`, you can fetch this cache data [GetAsync](https://github.com/adamfoneil/Dapper.CX/blob/master/Dapper.CX.Base/Abstract/DbDictionary.cs#L70).

Here's a hypothetical example that shows `SetEachAsync` caching some API call array results. The array is `drawings`. Each element has a `FullUrl` property that is assumed to be unique. Each element is converted to a hypothetical `ImportedDrawingInfo` object.

```csharp
await _cache.SetEachAsync(drawings, d => d.FullUrl, (dwg) => new ImportedDrawingInfo()
{
	CompanyId = CompanyId.Value,
	CompanyName = companyDictionary[CompanyId.Value],
	ProjectId = ProjectId.Value,
	ProjectName = projectDictionary[ProjectId.Value],
	DrawingSetId = DrawingSetId.Value,
	DrawingSetName = drawingSetDictionary[DrawingSetId.Value],
	RevInfo = dwg
});
```
I can later fetch the individual elements as needed using the `FullUrl` as a key. `GetAsync` will deserialize the json into its strongly-typed `ImportedDrawingInfo` form. In this example `request.SourceUri` represents the `FullUrl` key:

```csharp
var revInfo = await _cache.GetAsync<ImportedDrawingInfo>(request.SourceUri);
```

# Other Uses
Although I made this with API calls in mind, you can cache anything you can fetch or query with the `accessor` argument of [GetAsync](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs#L55). Results of database queries, for example, will work fine to cache as strong-typed json.

# DbCacheLibrary.DbCache [DbCache.cs](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs#L16)
## Properties
- string [KeyPrefix](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs#L25)
- [ObjectSource](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs#L10) [Source](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs#L27)
## Methods
- Task\<TValue\> [GetAsync](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs#L55)
 (string key, Func<Task<TValue>> accessor, TimeSpan maxAge)
- Task\<TValue\> [GetAsync](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs#L58)
 (string key, Func<Task<TValue>> accessor, DateTime expireAfterUtc)
- Task\<TValue\> [QueryAsync](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs#L61)
 (string key)
- Task\<Dictionary\<string, int\>\> [SetEachAsync](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs#L67)
 (IEnumerable<TItem> items, Func<TItem, string> keyAccessor)
- Task\<Dictionary\<string, int\>\> [SetEachAsync](https://github.com/adamfoneil/DbCache/blob/master/DbCache/DbCache.cs#L81)
 (IEnumerable<TSource> items, Func<TSource, string> keyAccessor, Func<TSource, TTarget> saveAsTarget)
