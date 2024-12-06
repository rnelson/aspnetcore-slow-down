using Microsoft.AspNetCore.Http;
using Nearform.AspNetCore.SlowDown.Helpers;
using Xunit.DependencyInjection;

namespace SlowDown.Tests.Helpers;

[Startup(typeof(Startup))]
[CollectionDefinition("CacheHelperTests", DisableParallelization = true)]
public class CacheHelperTests(CacheHelper cacheHelper)
    : IClassFixture<CacheHelper>
{
    private readonly CacheHelper _cache = cacheHelper ?? throw new ArgumentNullException(nameof(cacheHelper));
    private readonly string[] _tags = ["cacheHelperTests"];

    [Fact]
    [DisableParallelization]
    public async Task GetHttpRequest_CreatesNewItemInCache()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        HttpRequest? request = null;

        try
        {
            request = UnitTestHelperMethods.CreateXForwardedForHttpRequest();
        
            var count = await _cache.Get(request, tags: _tags);
            Assert.Equal(0, count);
        }
        finally
        {
            if (request != null) await _cache.Remove(request);
            CacheSemaphore.Semaphore.Release();
        }
    }
    
    [Fact]
    [DisableParallelization]
    public async Task GetHttpRequest_GetsItemFromCache()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        HttpRequest? request = null;

        try
        {
            const int expected = 8;
            request = UnitTestHelperMethods.CreateXForwardedForHttpRequest();
        
            var count = await _cache.Get(request, tags: _tags);
            Assert.Equal(0, count);
            
            await _cache.Set(request, expected, tags: _tags);
            count = await _cache.Get(request, tags: _tags);
            Assert.Equal(expected, count);
        }
        finally
        {
            if (request != null) await _cache.Remove(request);
            CacheSemaphore.Semaphore.Release();
        }
    }
    
    [Fact]
    [DisableParallelization]
    public async Task GetHttpRequest_RemoveAllWorks()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        HttpRequest? requestOne = null, requestTwo = null;
        var tags = new[] { "helperTest" };

        try
        {
            const string ipOne = "1.1.1.1";
            const string ipTwo = "2.2.2.2";
            const int expectedOne = 4;
            const int expectedTwo = 13;
            
            requestOne = UnitTestHelperMethods.CreateXForwardedForHttpRequest(ipOne);
            requestTwo = UnitTestHelperMethods.CreateXForwardedForHttpRequest(ipTwo);
        
            // Add a known value for IP 1 and make sure it's there. Use the method scope
            // list of tags instead of the class scope one.
            await _cache.Set(requestOne, expectedOne, tags: tags);
            var countOne = await _cache.Get(requestOne, tags: tags);
            Assert.Equal(expectedOne, countOne);
        
            // Add a known value for IP 2 and make sure it's there. Use the class scope list
            // of tags.
            await _cache.Set(requestTwo, expectedTwo, tags: _tags);
            var countTwo = await _cache.Get(requestTwo, tags: _tags);
            Assert.Equal(expectedTwo, countTwo);
            
            // Remove everything with the method scope tags.
            await _cache.RemoveAll(tags);
            
            // Ensure IP 1's entry is gone/default but that IP 2 is unchanged.
            countOne = await _cache.Get(requestOne);
            Assert.Equal(expectedOne, countOne);
            countTwo = await _cache.Get(requestTwo);
            Assert.Equal(expectedTwo, countTwo);
        }
        finally
        {
            if (requestOne != null) await _cache.Remove(requestOne);
            if (requestTwo != null) await _cache.Remove(requestTwo);
            CacheSemaphore.Semaphore.Release();
        }
    }
    
    [Fact]
    [DisableParallelization]
    public async Task GetHttpRequest_RemoveWorks()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        HttpRequest? requestOne = null, requestTwo = null;

        try
        {
            const string ipOne = "1.1.1.1";
            const string ipTwo = "2.2.2.2";
            const int expectedOne = 4;
            const int expectedTwo = 13;
            
            requestOne = UnitTestHelperMethods.CreateXForwardedForHttpRequest(ipOne);
            requestTwo = UnitTestHelperMethods.CreateXForwardedForHttpRequest(ipTwo);
        
            // Add a known value for IP 1 and make sure it's there.
            await _cache.Set(requestOne, expectedOne, tags: _tags);
            var countOne = await _cache.Get(requestOne, tags: _tags);
            Assert.Equal(expectedOne, countOne);
        
            // Add a known value for IP 2 and make sure it's there.
            await _cache.Set(requestTwo, expectedTwo, tags: _tags);
            var countTwo = await _cache.Get(requestTwo, tags: _tags);
            Assert.Equal(expectedTwo, countTwo);
            
            // Remove IP 2's entry
            await _cache.Remove(requestTwo);
            
            // Confirm IP 2's entry is now gone (aka returns the default of 0).
            countTwo = await _cache.Get(requestTwo, tags: _tags);
            Assert.Equal(0, countTwo);
            
            // Confirm IP 1's entry is unchanged.
            countOne = await _cache.Get(requestOne, tags: _tags);
            Assert.Equal(expectedOne, countOne);
        }
        finally
        {
            if (requestOne != null) await _cache.Remove(requestOne);
            if (requestTwo != null) await _cache.Remove(requestTwo);
            CacheSemaphore.Semaphore.Release();
        }
    }
    
    [Fact]
    [DisableParallelization]
    public async Task GetHttpRequest_SetUpdatesItemInCache()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        HttpRequest? request = null;

        try
        {
            const int expected = 5;
            request = UnitTestHelperMethods.CreateXForwardedForHttpRequest();
        
            var count = await _cache.Get(request, tags: _tags);
            Assert.Equal(0, count);
        
            await _cache.Set(request, expected, tags: _tags);
            count = await _cache.Get(request, tags: _tags);
            Assert.Equal(expected, count);
        }
        finally
        {
            if (request != null) await _cache.Remove(request);
            CacheSemaphore.Semaphore.Release();
        }
    }
}