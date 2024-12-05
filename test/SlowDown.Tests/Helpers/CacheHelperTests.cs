
using Nearform.AspNetCore.SlowDown;
using Nearform.AspNetCore.SlowDown.Helpers;

namespace SlowDown.Tests.Helpers;

public class CacheHelperTests(SlowDownOptions options, CacheHelper cacheHelper) : IClassFixture<SlowDownOptions>,
    IClassFixture<CacheHelper>
{
    private readonly SlowDownOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly CacheHelper _cache = cacheHelper ?? throw new ArgumentNullException(nameof(cacheHelper));

    [Fact]
    public async Task GetHttpRequest_CreatesNewItemInCache()
    {
        await CacheSemaphore.Semaphore.WaitAsync();

        try
        {
            var request = UnitTestHelperMethods.CreateXForwardedForHttpRequest();
        
            var count = await _cache.Get(request);
            Assert.Equal(0, count);
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }
    
    [Fact]
    public async Task GetHttpRequest_GetsItemFromCache()
    {
        await CacheSemaphore.Semaphore.WaitAsync();

        try
        {
            const int expected = 8;
            var request = UnitTestHelperMethods.CreateXForwardedForHttpRequest();
        
            var count = await _cache.Get(request);
            Assert.Equal(0, count);
        
            await _cache.Set(request, expected);
            count = await _cache.Get(request);
            Assert.Equal(expected, count);
        }
        finally
        {
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
    public async Task GetHttpRequest_SetUpdatesItemInCache()
    {
        await CacheSemaphore.Semaphore.WaitAsync();

        try
        {
            const int expected = 5;
            var request = UnitTestHelperMethods.CreateXForwardedForHttpRequest();
        
            var count = await _cache.Get(request);
            Assert.Equal(0, count);
        
            await _cache.Set(request, expected);
            count = await _cache.Get(request);
            Assert.Equal(expected, count);
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }
}