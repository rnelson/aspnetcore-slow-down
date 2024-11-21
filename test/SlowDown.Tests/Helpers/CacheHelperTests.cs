using Nearform.AspNetCore.SlowDown.Helpers;

namespace SlowDown.Tests.Helpers;

public class CacheHelperTests
{
    [Fact]
    public async Task GetHttpRequest_CreatesNewItemInCache()
    {
        await CacheSemaphore.Semaphore.WaitAsync();

        try
        {
            var (_, request) = UnitTestHelperMethods.Setup();
        
            var (count, _) = await CacheHelper.Get(request);
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
            var (_, request) = UnitTestHelperMethods.Setup();
        
            var (count, _) = await CacheHelper.Get(request);
            Assert.Equal(0, count);
        
            await CacheHelper.Set(request, expected);
            (count, _) = await CacheHelper.Get(request);
            Assert.Equal(expected, count);
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }
    
    [Fact]
    public async Task GetHttpRequest_SetUpdatesItemInCache()
    {
        await CacheSemaphore.Semaphore.WaitAsync();

        try
        {
            const int expected = 5;
            var (_, request) = UnitTestHelperMethods.Setup();
        
            var (count, _) = await CacheHelper.Get(request);
            Assert.Equal(0, count);
        
            await CacheHelper.Set(request, expected);
            (count, _) = await CacheHelper.Get(request);
            Assert.Equal(expected, count);
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }
}