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

        try
        {
            var request = UnitTestHelperMethods.CreateXForwardedForHttpRequest();
        
            var count = await _cache.Get(request, tags: _tags);
            Assert.Equal(0, count);
        }
        finally
        {
            await _cache.RemoveAll(_tags);
            CacheSemaphore.Semaphore.Release();
        }
    }
    
    [Fact]
    [DisableParallelization]
    public async Task GetHttpRequest_GetsItemFromCache()
    {
        await CacheSemaphore.Semaphore.WaitAsync();

        try
        {
            const int expected = 8;
            var request = UnitTestHelperMethods.CreateXForwardedForHttpRequest();
        
            var count = await _cache.Get(request, tags: _tags);
            Assert.Equal(0, count);
            
            await _cache.Set(request, expected, tags: _tags);
            count = await _cache.Get(request, tags: _tags);
            Assert.Equal(expected, count);
        }
        finally
        {
            await _cache.RemoveAll(_tags);
            CacheSemaphore.Semaphore.Release();
        }
    }
    
    // [Fact]
    // public async Task GetHttpRequest_GetsNullWithNoCache()
    // {
    //     await CacheSemaphore.Semaphore.WaitAsync();
    //
    //     try
    //     {
    //         var (_, request) = UnitTestHelperMethods.Setup();
    //     
    //         var count = await _cache.Get(request);
    //         Assert.Equal(0, count);
    //     }
    //     finally
    //     {
    //         CacheSemaphore.Semaphore.Release();
    //     }
    // }
    
    [Fact]
    [DisableParallelization]
    public async Task GetHttpRequest_SetUpdatesItemInCache()
    {
        await CacheSemaphore.Semaphore.WaitAsync();

        try
        {
            const int expected = 5;
            var request = UnitTestHelperMethods.CreateXForwardedForHttpRequest();
        
            var count = await _cache.Get(request, tags: _tags);
            Assert.Equal(0, count);
        
            await _cache.Set(request, expected, tags: _tags);
            count = await _cache.Get(request, tags: _tags);
            Assert.Equal(expected, count);
        }
        finally
        {
            await _cache.RemoveAll(_tags);
            CacheSemaphore.Semaphore.Release();
        }
    }
}