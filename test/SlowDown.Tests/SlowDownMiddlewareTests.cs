using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Nearform.AspNetCore.SlowDown;
using Nearform.AspNetCore.SlowDown.Helpers;

namespace SlowDown.Tests;

[SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeNotEvident")]
public class SlowDownMiddlewareTests
{
    [Fact]
    public async Task Constructor_Works()
    {
        await CacheSemaphore.Semaphore.WaitAsync();

        try
        {
            _ = UnitTestHelperMethods.CreateSlowDownMiddleware();
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }

    [Fact]
    public async Task HandleSlowDown_AddedCorrectHeadersAfterLimit()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        
        try
        {
            SlowDownOptions.CurrentOptions = new()
            {
                AddHeaders = true,
                Cache = UnitTestHelperMethods.CreateCache(),
                DelayAfter = 10
            };

            // Set the current number of requests to 10. Calling InvokeAsync()
            // will increment the count to 11 before doing any math.
            await CacheHelper.Set("127.0.0.1", 10);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var middleware = UnitTestHelperMethods.CreateSlowDownMiddleware();

            await middleware.InvokeAsync(context);

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
            SlowDownOptions.CurrentOptions = new()
            {
                AddHeaders = true,
                Cache = UnitTestHelperMethods.CreateCache(),
                DelayAfter = 50
            };

            // Set the current number of requests to 10. Calling InvokeAsync()
            // will increment the count to 11 before doing any math.
            await CacheHelper.Set("127.0.0.1", 10);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var middleware = UnitTestHelperMethods.CreateSlowDownMiddleware();

            await middleware.InvokeAsync(context);

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
            SlowDownOptions.CurrentOptions = new()
            {
                Cache = UnitTestHelperMethods.CreateCache(),
                DelayAfter = 10
            };

            await CacheHelper.Set("127.0.0.1", 9);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var middleware = UnitTestHelperMethods.CreateSlowDownMiddleware();

            var timer = Stopwatch.StartNew();
            await middleware.InvokeAsync(context);
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
            SlowDownOptions.CurrentOptions = new()
            {
                Cache = UnitTestHelperMethods.CreateCache(),
                Delay = 2000,
                DelayAfter = 10
            };

            await CacheHelper.Set("127.0.0.1", 11);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var middleware = UnitTestHelperMethods.CreateSlowDownMiddleware();

            var timer = Stopwatch.StartNew();
            await middleware.InvokeAsync(context);
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
            SlowDownOptions.CurrentOptions = new()
            {
                AddHeaders = false,
                Cache = UnitTestHelperMethods.CreateCache(),
                DelayAfter = 500
            };

            await CacheHelper.Set("127.0.0.1", 84);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var middleware = UnitTestHelperMethods.CreateSlowDownMiddleware();

            await middleware.InvokeAsync(context);

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
    public async Task HandleSlowDown_RemainingIsZeroWithNoWindow()
    {
        await CacheSemaphore.Semaphore.WaitAsync();
        
        try
        {
            const int startingRequestCount = 300;
            const int delay = 50;
            const int delayAfter = 100;
            const int expectedDelay = (startingRequestCount - delayAfter) * delay + delay;
            
            SlowDownOptions.CurrentOptions = new()
            {
                AddHeaders = true,
                Cache = UnitTestHelperMethods.CreateCache(),
                Delay = delay,
                DelayAfter = delayAfter,
                FakeDelay = true,
                TimeWindow = 1000
            };

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var middleware = UnitTestHelperMethods.CreateSlowDownMiddleware();
            
            // Set the current number of requests to 300. Expiration will be `TimeWindow`
            // from now.
            await CacheHelper.Set("127.0.0.1", startingRequestCount);
            
            // Wait one second after the window is set to expire. Between the added 1000ms
            // and processing time for the above, we should be past the expiration time.
            await Task.Delay(SlowDownOptions.CurrentOptions.TimeWindow + 1000);

            // Trigger a single request, and make sure that things got reset.
            await middleware.InvokeAsync(context);

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
    public async Task InvokeAsync_WorksDefault()
    {
        await CacheSemaphore.Semaphore.WaitAsync();

        try
        {
            var context = UnitTestHelperMethods.CreateHttpContext();
            var middleware = UnitTestHelperMethods.CreateSlowDownMiddleware();

            await middleware.InvokeAsync(context);
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
            SlowDownOptions.CurrentOptions = new()
            {
                SlowDownEnabled = false
            };
            
            var context = UnitTestHelperMethods.CreateHttpContext();
            var middleware = UnitTestHelperMethods.CreateSlowDownMiddleware();

            await middleware.InvokeAsync(context);
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }
}
