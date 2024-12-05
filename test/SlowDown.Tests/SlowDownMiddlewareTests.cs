using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nearform.AspNetCore.SlowDown;
using Nearform.AspNetCore.SlowDown.Helpers;
using Xunit.DependencyInjection;

namespace SlowDown.Tests;

[Startup(typeof(Startup))]
[SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeNotEvident")]
public class SlowDownMiddlewareTests
{
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
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.AddHeaders = true;
                options.DelayAfter = 10;
            });
            
            // Start the test server.
            var host = await builder.StartAsync();
            var client = host.GetTestClient();
            
            // Set the current number of requests to 10. Calling InvokeAsync()
            // will increment the count to 11 before doing any math.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 10);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            var response = await client.SendAsync(message);

            Assert.True(response.Headers.Contains(Constants.DelayHeader));
            Assert.True(response.Headers.Contains(Constants.LimitHeader));
            Assert.True(response.Headers.Contains(Constants.RemainingHeader));

            Assert.Equal(1000, int.Parse(response.Headers.GetValues(Constants.DelayHeader).First()));
            Assert.Equal(10, int.Parse(response.Headers.GetValues(Constants.LimitHeader).First()));
            Assert.Equal(0, int.Parse(response.Headers.GetValues(Constants.RemainingHeader).First()));
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
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.AddHeaders = true;
                options.DelayAfter = 50;
            });
            
            // Start the test server.
            var host = await builder.StartAsync();
            var client = host.GetTestClient();
            
            // Set the current number of requests to 10. Calling InvokeAsync()
            // will increment the count to 11 before doing any math.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 10);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            var response = await client.SendAsync(message);

            Assert.True(response.Headers.Contains(Constants.DelayHeader));
            Assert.True(response.Headers.Contains(Constants.LimitHeader));
            Assert.True(response.Headers.Contains(Constants.RemainingHeader));

            Assert.Equal(0, int.Parse(response.Headers.GetValues(Constants.DelayHeader).First()));
            Assert.Equal(50, int.Parse(response.Headers.GetValues(Constants.LimitHeader).First()));
            Assert.Equal(39, int.Parse(response.Headers.GetValues(Constants.RemainingHeader).First()));
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
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.DelayAfter = 10;
            });
            
            // Start the test server.
            var host = await builder.StartAsync();
            var client = host.GetTestClient();
            
            // Set the current number of requests to 10. Calling InvokeAsync()
            // will increment the count to 11 before doing any math.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 9);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            var timer = Stopwatch.StartNew();
            _ = await client.SendAsync(message);
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
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.Delay = 2000;
                options.DelayAfter = 10;
            });
            
            // Start the test server.
            var host = await builder.StartAsync();
            var client = host.GetTestClient();
            
            // Set the current number of requests to 10. Calling InvokeAsync()
            // will increment the count to 11 before doing any math.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 11);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            var timer = Stopwatch.StartNew();
            _ = await client.SendAsync(message);
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
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.AddHeaders = false;
                options.DelayAfter = 500;
            });
            
            // Start the test server.
            var host = await builder.StartAsync();
            var client = host.GetTestClient();
            
            // Set the current number of requests to 10. Calling InvokeAsync()
            // will increment the count to 11 before doing any math.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 84);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            var response = await client.SendAsync(message);

            Assert.False(response.Headers.Contains(Constants.DelayHeader));
            Assert.False(response.Headers.Contains(Constants.LimitHeader));
            Assert.False(response.Headers.Contains(Constants.RemainingHeader));
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
            
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.DelayAfter = 1;
                options.FakeDelay = true;
                options.OnLimitReached = _ => flag = true;
            });
            
            // Start the test server.
            var host = await builder.StartAsync();
            var client = host.GetTestClient();
            
            // Set the current number of requests to 10. Calling InvokeAsync()
            // will increment the count to 11 before doing any math.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 5);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            Assert.False(flag);
            
            // Send the request.
            _ = await client.SendAsync(message);

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
            
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.AddHeaders = true;
                options.Delay = delay;
                options.DelayAfter = delayAfter;
                options.FakeDelay = true;
                options.TimeWindow = 1000;
            });
            
            // Start the test server.
            var host = await builder.StartAsync();
            var client = host.GetTestClient();
            
            // Grab the configured options.
            var options = host.Services.GetRequiredService<SlowDownOptions>();
            
            // Set the current number of requests to 10. Calling InvokeAsync()
            // will increment the count to 11 before doing any math.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", startingRequestCount);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Wait one second after the window is set to expire. Between the added 1000ms
            // and processing time for the above, we should be past the expiration time.
            await Task.Delay(options.TimeWindow + 1000);
            
            // Send the request.
            var response = await client.SendAsync(message);

            Assert.True(response.Headers.Contains(Constants.DelayHeader));
            Assert.True(response.Headers.Contains(Constants.LimitHeader));
            Assert.True(response.Headers.Contains(Constants.RemainingHeader));

            Assert.Equal(expectedDelay, int.Parse(response.Headers.GetValues(Constants.DelayHeader).First()));
            Assert.Equal(100, int.Parse(response.Headers.GetValues(Constants.LimitHeader).First()));
            Assert.Equal(0, int.Parse(response.Headers.GetValues(Constants.RemainingHeader).First()));
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
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.DelayAfter = 6;
                options.FakeDelay = true;
                options.SkipFailedRequests = true;
            });
            
            // Start the test server.
            var host = await builder.StartAsync();
            var client = host.GetTestClient();
            
            // Set the current number of requests to 10. Calling InvokeAsync()
            // will increment the count to 11 before doing any math.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 5);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            message.RequestUri = new Uri("/does_not_exist");
            
            // Send the request.
            var response = await client.SendAsync(message);

            Assert.True(response.Headers.Contains(Constants.DelayHeader));
            Assert.True(response.Headers.Contains(Constants.LimitHeader));
            Assert.True(response.Headers.Contains(Constants.RemainingHeader));

            Assert.Equal(0, int.Parse(response.Headers.GetValues(Constants.DelayHeader).First()));
            Assert.Equal(6, int.Parse(response.Headers.GetValues(Constants.LimitHeader).First()));
            Assert.Equal(1, int.Parse(response.Headers.GetValues(Constants.RemainingHeader).First()));
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
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.DelayAfter = 6;
                options.FakeDelay = true;
                options.SkipSuccessfulRequests = true;
            });
            
            // Start the test server.
            var host = await builder.StartAsync();
            var client = host.GetTestClient();
            
            // Set the current number of requests to 10. Calling InvokeAsync()
            // will increment the count to 11 before doing any math.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 5);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            message.RequestUri = new Uri("/");
            
            // Send the request.
            var response = await client.SendAsync(message);

            Assert.True(response.Headers.Contains(Constants.DelayHeader));
            Assert.True(response.Headers.Contains(Constants.LimitHeader));
            Assert.True(response.Headers.Contains(Constants.RemainingHeader));

            Assert.Equal(0, int.Parse(response.Headers.GetValues(Constants.DelayHeader).First()));
            Assert.Equal(6, int.Parse(response.Headers.GetValues(Constants.LimitHeader).First()));
            Assert.Equal(1, int.Parse(response.Headers.GetValues(Constants.RemainingHeader).First()));
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
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.DelayAfter = 6;
                options.FakeDelay = true;
                options.Skip = _ => true;
            });
            
            // Start the test server.
            var host = await builder.StartAsync();
            var client = host.GetTestClient();
            
            // Set the current number of requests to 10. Calling InvokeAsync()
            // will increment the count to 11 before doing any math.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 5);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            var response = await client.SendAsync(message);

            Assert.True(response.Headers.Contains(Constants.DelayHeader));
            Assert.True(response.Headers.Contains(Constants.LimitHeader));
            Assert.True(response.Headers.Contains(Constants.RemainingHeader));

            Assert.Equal(0, int.Parse(response.Headers.GetValues(Constants.DelayHeader).First()));
            Assert.Equal(6, int.Parse(response.Headers.GetValues(Constants.LimitHeader).First()));
            Assert.Equal(1, int.Parse(response.Headers.GetValues(Constants.RemainingHeader).First()));
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
            // Start the test server.
            var builder = UnitTestHelperMethods.CreateWebHostBuilder();
            var host = await builder.StartAsync();
            var client = host.GetTestClient();

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            _ = await client.SendAsync(message);
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
            // Start the test server.
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options => { options.SlowDownEnabled = false; });
            var host = await builder.StartAsync();
            var client = host.GetTestClient();

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);

            // Send the request.
            _ = await client.SendAsync(message);
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }
}
