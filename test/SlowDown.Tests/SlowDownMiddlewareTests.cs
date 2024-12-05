using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Nearform.AspNetCore.SlowDown;
using Nearform.AspNetCore.SlowDown.Helpers;

namespace SlowDown.Tests;

[SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeNotEvident")]
public class SlowDownMiddlewareTests(SlowDownOptions options, CacheHelper cacheHelper, SlowDownMiddleware middleware)
    : IClassFixture<SlowDownOptions>, IClassFixture<CacheHelper>, IClassFixture<SlowDownMiddleware>
{
    private readonly SlowDownOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly CacheHelper _cache = cacheHelper ?? throw new ArgumentNullException(nameof(cacheHelper));
    private readonly SlowDownMiddleware _middleware = middleware ?? throw new ArgumentNullException(nameof(middleware));
    
    // [Fact]
    // public async Task Constructor_Works()
    // {
    //     await CacheSemaphore.Semaphore.WaitAsync();
    //
    //     try
    //     {
    //         _ = UnitTestHelperMethods.CreateSlowDownMiddleware();
    //     }
    //     finally
    //     {
    //         CacheSemaphore.Semaphore.Release();
    //     }
    // }

    [Fact]
    public async Task HandleSlowDown_AddedCorrectHeadersAfterLimit()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        
        try
        {
            _options.AddHeaders = true;
            _options.DelayAfter = 10;

            // Set the current number of requests to 10. Calling InvokeAsync()
            // will increment the count to 11 before doing any math.
            await _cache.Set("127.0.0.1", 10);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();

            await _middleware.InvokeAsync(context);

            Assert.True(context.Response.Headers.ContainsKey(Constants.DelayHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.LimitHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.RemainingHeader));

            Assert.Equal(1000, int.Parse(context.Response.Headers[Constants.DelayHeader].ToString()));
            Assert.Equal(10, int.Parse(context.Response.Headers[Constants.LimitHeader].ToString()));
            Assert.Equal(0, int.Parse(context.Response.Headers[Constants.RemainingHeader].ToString()));
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }

    [Fact]
    public async Task HandleSlowDown_AddedCorrectHeadersBeforeLimit()
    {
        await CacheSemaphore.Semaphore.WaitAsync();

        try
        {
            _options.AddHeaders = true;
            _options.DelayAfter = 50;

            // Set the current number of requests to 10. Calling InvokeAsync()
            // will increment the count to 11 before doing any math.
            await _cache.Set("127.0.0.1", 10);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();

            await _middleware.InvokeAsync(context);

            Assert.True(context.Response.Headers.ContainsKey(Constants.DelayHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.LimitHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.RemainingHeader));

            Assert.Equal(0, int.Parse(context.Response.Headers[Constants.DelayHeader].ToString()));
            Assert.Equal(50, int.Parse(context.Response.Headers[Constants.LimitHeader].ToString()));
            Assert.Equal(39, int.Parse(context.Response.Headers[Constants.RemainingHeader].ToString()));
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }

    [Fact]
    public async Task HandleSlowDown_DelayNotAddedUnnecessarily()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        
        try
        {
            _options.DelayAfter = 10;

            await _cache.Set("127.0.0.1", 9);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();

            var timer = Stopwatch.StartNew();
            await _middleware.InvokeAsync(context);
            timer.Stop();
            
            Assert.True(timer.ElapsedMilliseconds < 1000);
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }

    [Fact]
    public async Task HandleSlowDown_DelayWorks()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        
        try
        {
            _options.Delay = 2000;
            _options.DelayAfter = 10;

            await _cache.Set("127.0.0.1", 11);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            
            var timer = Stopwatch.StartNew();
            await _middleware.InvokeAsync(context);
            timer.Stop();
            
            Assert.True(timer.ElapsedMilliseconds > 1000);
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }

    [Fact]
    public async Task HandleSlowDown_ExcludesHeadersWhenDisabled()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        
        try
        {
            _options.AddHeaders = false;
            _options.DelayAfter = 500;

            await _cache.Set("127.0.0.1", 84);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();

            await _middleware.InvokeAsync(context);

            Assert.False(context.Response.Headers.ContainsKey(Constants.DelayHeader));
            Assert.False(context.Response.Headers.ContainsKey(Constants.LimitHeader));
            Assert.False(context.Response.Headers.ContainsKey(Constants.RemainingHeader));
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }

    [Fact]
    public async Task HandleSlowDown_OnLimitReached_Works()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        
        try
        {
            var flag = false;
            
            _options.DelayAfter = 1;
            _options.FakeDelay = true;
            _options.OnLimitReached = _ => flag = true;

            await _cache.Set("127.0.0.1", 5);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            
            Assert.False(flag);
            await _middleware.InvokeAsync(context);
            Assert.True(flag);
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }

    [Fact]
    public async Task HandleSlowDown_RemainingIsZeroWithNoWindow()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        
        try
        {
            const int startingRequestCount = 300;
            const int delay = 50;
            const int delayAfter = 100;
            const int expectedDelay = (startingRequestCount - delayAfter) * delay + delay;
            
            _options.AddHeaders = true;
            _options.Delay = delay;
            _options.DelayAfter = delayAfter;
            _options.FakeDelay = true;
            _options.TimeWindow = 1000;

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            
            // Set the current number of requests to 300. Expiration will be `TimeWindow`
            // from now.
            await _cache.Set("127.0.0.1", startingRequestCount);
            
            // Wait one second after the window is set to expire. Between the added 1000ms
            // and processing time for the above, we should be past the expiration time.
            await Task.Delay(_options.TimeWindow + 1000);

            // Trigger a single request, and make sure that things got reset.
            await _middleware.InvokeAsync(context);

            Assert.True(context.Response.Headers.ContainsKey(Constants.DelayHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.LimitHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.RemainingHeader));

            Assert.Equal(expectedDelay, int.Parse(context.Response.Headers[Constants.DelayHeader].ToString()));
            Assert.Equal(100, int.Parse(context.Response.Headers[Constants.LimitHeader].ToString()));
            Assert.Equal(0, int.Parse(context.Response.Headers[Constants.RemainingHeader].ToString()));
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }

    [Fact]
    public async Task HandleSlowDown_SkipFailedRequests_Works()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        
        try
        {
            _options.DelayAfter = 6;
            _options.FakeDelay = true;
            _options.SkipFailedRequests = true;

            await _cache.Set("127.0.0.1", 5);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            
            context.Response.StatusCode = 404;
            await _middleware.InvokeAsync(context);

            Assert.True(context.Response.Headers.ContainsKey(Constants.DelayHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.LimitHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.RemainingHeader));

            Assert.Equal(0, int.Parse(context.Response.Headers[Constants.DelayHeader].ToString()));
            Assert.Equal(6, int.Parse(context.Response.Headers[Constants.LimitHeader].ToString()));
            Assert.Equal(1, int.Parse(context.Response.Headers[Constants.RemainingHeader].ToString()));
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }

    [Fact]
    public async Task HandleSlowDown_SkipSuccessfulRequests_Works()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        
        try
        {
            _options.DelayAfter = 6;
            _options.FakeDelay = true;
            _options.SkipSuccessfulRequests = true;

            await _cache.Set("127.0.0.1", 5);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            
            context.Response.StatusCode = 200;
            await _middleware.InvokeAsync(context);

            Assert.True(context.Response.Headers.ContainsKey(Constants.DelayHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.LimitHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.RemainingHeader));

            Assert.Equal(0, int.Parse(context.Response.Headers[Constants.DelayHeader].ToString()));
            Assert.Equal(6, int.Parse(context.Response.Headers[Constants.LimitHeader].ToString()));
            Assert.Equal(1, int.Parse(context.Response.Headers[Constants.RemainingHeader].ToString()));
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }

    [Fact]
    public async Task HandleSlowDown_Skip_Works()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        
        try
        {
            _options.DelayAfter = 6;
            _options.FakeDelay = true;
            _options.Skip = _ => true;

            await _cache.Set("127.0.0.1", 5);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            
            await _middleware.InvokeAsync(context);

            Assert.True(context.Response.Headers.ContainsKey(Constants.DelayHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.LimitHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.RemainingHeader));

            Assert.Equal(0, int.Parse(context.Response.Headers[Constants.DelayHeader].ToString()));
            Assert.Equal(6, int.Parse(context.Response.Headers[Constants.LimitHeader].ToString()));
            Assert.Equal(1, int.Parse(context.Response.Headers[Constants.RemainingHeader].ToString()));
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }
    
    [Fact]
    public async Task InvokeAsync_WorksDefault()
    {
        await CacheSemaphore.Semaphore.WaitAsync();

        try
        {
            var context = UnitTestHelperMethods.CreateHttpContext();

            await _middleware.InvokeAsync(context);
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }
    
    [Fact]
    public async Task InvokeAsync_WorksWithSlowDownDisabled()
    {
        await CacheSemaphore.Semaphore.WaitAsync();

        try
        {
            _options.SlowDownEnabled = false;
            
            var context = UnitTestHelperMethods.CreateHttpContext();

            await _middleware.InvokeAsync(context);
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }
}
