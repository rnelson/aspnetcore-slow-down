using Nearform.AspNetCore.SlowDown;

namespace SlowDown.Tests;

public class SlowDownMiddlewareTests
{
    private static readonly SemaphoreSlim Semaphore = new(1,1);
    
    [Fact]
    public void Constructor_Works()
    {
        _ = UnitTestHelperMethods.CreateSlowDownMiddleware();
    }
    
    [Fact]
    public async Task InvokeAsync_WorksDefault()
    {
        var context = UnitTestHelperMethods.CreateHttpContext();
        var middleware = UnitTestHelperMethods.CreateSlowDownMiddleware();

        await middleware.InvokeAsync(context);
    }
    
    [Fact]
    public async Task InvokeAsync_WorksWithSlowDownDisabled()
    {
        var context = UnitTestHelperMethods.CreateHttpContext();
        var middleware = UnitTestHelperMethods.CreateSlowDownMiddleware();

        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task HandleSlowDown_AddedCorrectHeadersBeforeLimit()
    {
        await Semaphore.WaitAsync();

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
            await SlowDownOptions.CurrentOptions.Cache.SetAsync("127.0.0.1", 10);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var middleware = UnitTestHelperMethods.CreateSlowDownMiddleware();

            await middleware.InvokeAsync(context);

            Assert.True(context.Response.Headers.ContainsKey(Constants.DelayHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.LimitHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.RemainingHeader));

            Assert.Equal(0, int.Parse(context.Response.Headers[Constants.DelayHeader].ToString()));
            Assert.Equal(50, int.Parse(context.Response.Headers[Constants.LimitHeader].ToString()));
            Assert.Equal(0, int.Parse(context.Response.Headers[Constants.RemainingHeader].ToString()));
        }
        finally
        {
            Semaphore.Release();
        }
    }

    [Fact]
    public async Task HandleSlowDown_AddedCorrectHeadersAfterLimit()
    {
        await Semaphore.WaitAsync();
        
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
            await SlowDownOptions.CurrentOptions.Cache.SetAsync("127.0.0.1", 10);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var middleware = UnitTestHelperMethods.CreateSlowDownMiddleware();

            await middleware.InvokeAsync(context);

            Assert.True(context.Response.Headers.ContainsKey(Constants.DelayHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.LimitHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.RemainingHeader));

            Assert.Equal(1000, int.Parse(context.Response.Headers[Constants.DelayHeader].ToString()));
            Assert.Equal(10, int.Parse(context.Response.Headers[Constants.LimitHeader].ToString()));
            Assert.Equal(-1, int.Parse(context.Response.Headers[Constants.RemainingHeader].ToString()));
        }
        finally
        {
            Semaphore.Release();
        }
    }

    [Fact]
    public async Task HandleSlowDown_RemainingIsZeroWithNoWindow()
    {
        await Semaphore.WaitAsync();
        
        try
        {
            SlowDownOptions.CurrentOptions = new()
            {
                AddHeaders = true,
                Cache = UnitTestHelperMethods.CreateCache(),
                DelayAfter = 1,
                TimeWindow = 0
            };

            // Set the current number of requests to 10. Calling InvokeAsync()
            // will increment the count to 11 before doing any math.
            await SlowDownOptions.CurrentOptions.Cache.SetAsync("127.0.0.1", 300);

            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var middleware = UnitTestHelperMethods.CreateSlowDownMiddleware();

            await middleware.InvokeAsync(context);

            Assert.True(context.Response.Headers.ContainsKey(Constants.DelayHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.LimitHeader));
            Assert.True(context.Response.Headers.ContainsKey(Constants.RemainingHeader));

            Assert.Equal(0, int.Parse(context.Response.Headers[Constants.DelayHeader].ToString()));
            Assert.Equal(1, int.Parse(context.Response.Headers[Constants.LimitHeader].ToString()));
            Assert.Equal(-300, int.Parse(context.Response.Headers[Constants.RemainingHeader].ToString()));
        }
        finally
        {
            Semaphore.Release();
        }
    }
}
