using Nearform.AspNetCore.SlowDown.Helpers;

namespace SlowDown.Tests.Helpers;

public class CacheHelperTests
{
    [Fact]
    public async Task GetHttpRequest_CreatesNewItemInCache()
    {
        var (_, request) = UnitTestHelperMethods.Setup();
        
        var (count, _) = await CacheHelper.Get(request);
        Assert.Equal(0, count);
    }
    
    [Fact]
    public async Task GetHttpRequest_GetsItemFromCache()
    {
        const int expected = 8;
        var (_, request) = UnitTestHelperMethods.Setup();
        
        var (count, _) = await CacheHelper.Get(request);
        Assert.Equal(0, count);
        
        await CacheHelper.Set(request, expected);
        (count, _) = await CacheHelper.Get(request);
        Assert.Equal(expected, count);
    }
    
    [Fact]
    public async Task GetHttpRequest_SetUpdatesItemInCache()
    {
        const int expected = 5;
        var (_, request) = UnitTestHelperMethods.Setup();
        
        var (count, _) = await CacheHelper.Get(request);
        Assert.Equal(0, count);
        
        await CacheHelper.Set(request, expected);
        (count, _) = await CacheHelper.Get(request);
        Assert.Equal(expected, count);
    }
    
    // TODO: test cache expiration
}