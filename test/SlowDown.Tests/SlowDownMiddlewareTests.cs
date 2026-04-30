using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Libexec.AspNetCore.SlowDown;
using Libexec.AspNetCore.SlowDown.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.DependencyInjection;

namespace SlowDown.Tests;

[Startup(typeof(Startup))]
[SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeNotEvident")]
public class SlowDownMiddlewareTests
{
    [Fact]
    public async Task HandleSlowDown_AddedCorrectHeadersAfterLimit()
    {
        await CacheSemaphore.Semaphore.WaitAsync(TestContext.Current.CancellationToken);
        
        try
        {
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.AddHeaders = true;
                options.DelayAfter = 10;
            });
            
            // Start the test server.
            var host = await builder.StartAsync(cancellationToken: TestContext.Current.CancellationToken);
            var client = host.GetTestClient();
            
            // Set a current count for the user.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 10);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            var response = await client.SendAsync(message, TestContext.Current.CancellationToken);

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
        await CacheSemaphore.Semaphore.WaitAsync(TestContext.Current.CancellationToken);
        
        try
        {
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.AddHeaders = true;
                options.DelayAfter = 50;
            });
            
            // Start the test server.
            var host = await builder.StartAsync(TestContext.Current.CancellationToken);
            var client = host.GetTestClient();
            
            // Set a current count for the user.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 10);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            var response = await client.SendAsync(message, TestContext.Current.CancellationToken);

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
    public async Task HandleSlowDown_CalculateDelayReturnsZeroWhenTimeWindowIsZero()
    {
        await CacheSemaphore.Semaphore.WaitAsync(TestContext.Current.CancellationToken);
        
        try
        {
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.MaxDelay = 0;
            });
            
            // Start the test server.
            var host = await builder.StartAsync(TestContext.Current.CancellationToken);
            var client = host.GetTestClient();
            
            // Set a current count for the user.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 500);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            var response = await client.SendAsync(message, TestContext.Current.CancellationToken);

            Assert.True(response.Headers.Contains(Constants.DelayHeader));
            Assert.Equal(0, int.Parse(response.Headers.GetValues(Constants.DelayHeader).First()));
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }

    [Fact]
    public async Task HandleSlowDown_DelayNotAddedUnnecessarily()
    {
        await CacheSemaphore.Semaphore.WaitAsync(TestContext.Current.CancellationToken);
        
        try
        {
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.DelayAfter = 10;
            });
            
            // Start the test server.
            var host = await builder.StartAsync(TestContext.Current.CancellationToken);
            var client = host.GetTestClient();
            
            // Set a current count for the user.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 9);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            var timer = Stopwatch.StartNew();
            _ = await client.SendAsync(message, TestContext.Current.CancellationToken);
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
        await CacheSemaphore.Semaphore.WaitAsync(TestContext.Current.CancellationToken);
        
        try
        {
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.Delay = 2000;
                options.DelayAfter = 10;
            });
            
            // Start the test server.
            var host = await builder.StartAsync(TestContext.Current.CancellationToken);
            var client = host.GetTestClient();
            
            // Set a current count for the user.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 11);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            var timer = Stopwatch.StartNew();
            _ = await client.SendAsync(message, TestContext.Current.CancellationToken);
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
        await CacheSemaphore.Semaphore.WaitAsync(TestContext.Current.CancellationToken);
        
        try
        {
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.AddHeaders = false;
                options.DelayAfter = 500;
            });
            
            // Start the test server.
            var host = await builder.StartAsync(TestContext.Current.CancellationToken);
            var client = host.GetTestClient();
            
            // Set a current count for the user.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 84);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            var response = await client.SendAsync(message, TestContext.Current.CancellationToken);

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
        await CacheSemaphore.Semaphore.WaitAsync(TestContext.Current.CancellationToken);
        
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
            var host = await builder.StartAsync(TestContext.Current.CancellationToken);
            var client = host.GetTestClient();
            
            // Set a current count for the user.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 5);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            Assert.False(flag);
            
            // Send the request.
            _ = await client.SendAsync(message, TestContext.Current.CancellationToken);

            Assert.True(flag);
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }

    [Fact]
    public async Task HandleSlowDown_ExpectedDelayIsCorrectlyCalculated()
    {
        await CacheSemaphore.Semaphore.WaitAsync(TestContext.Current.CancellationToken);
        
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
            });
            
            // Start the test server.
            var host = await builder.StartAsync(TestContext.Current.CancellationToken);
            var client = host.GetTestClient();
            
            // Set a current count for the user.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", startingRequestCount);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            var response = await client.SendAsync(message, TestContext.Current.CancellationToken);

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
    public async Task HandleSlowDown_RemainingIsZeroWithNoWindow()
    {
        await CacheSemaphore.Semaphore.WaitAsync(TestContext.Current.CancellationToken);
        
        try
        {
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.AddHeaders = true;
                options.Delay = 50;
                options.DelayAfter = 100;
                options.FakeDelay = true;
                options.TimeWindow = 0;
            });
            
            // Start the test server.
            var host = await builder.StartAsync(TestContext.Current.CancellationToken);
            var client = host.GetTestClient();
            
            // Set a current count for the user.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 300);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            var response = await client.SendAsync(message, TestContext.Current.CancellationToken);

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
    public async Task HandleSlowDown_SkipFailedRequests_Works()
    {
        await CacheSemaphore.Semaphore.WaitAsync(TestContext.Current.CancellationToken);
        
        try
        {
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.DelayAfter = 6;
                options.FakeDelay = true;
                options.SkipFailedRequests = true;
            }, app =>
            {
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/err", context =>
                    {
                        context.Response.StatusCode = 500;
                        return Task.CompletedTask;
                    });
                });
            });
            
            // Start the test server.
            var host = await builder.StartAsync(TestContext.Current.CancellationToken);
            var client = host.GetTestClient();
            
            // Set a current count for the user.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 5);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            message.RequestUri = new Uri("/err", UriKind.Relative);
            
            // Send the request.
            var response = await client.SendAsync(message, TestContext.Current.CancellationToken);

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
        await CacheSemaphore.Semaphore.WaitAsync(TestContext.Current.CancellationToken);
        
        try
        {
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.DelayAfter = 6;
                options.FakeDelay = true;
                options.SkipSuccessfulRequests = true;
            });
            
            // Start the test server.
            var host = await builder.StartAsync(TestContext.Current.CancellationToken);
            var client = host.GetTestClient();
            
            // Set a current count for the user.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 5);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            message.RequestUri = new Uri("/", UriKind.Relative);
            
            // Send the request.
            var response = await client.SendAsync(message, TestContext.Current.CancellationToken);

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
        await CacheSemaphore.Semaphore.WaitAsync(TestContext.Current.CancellationToken);
        
        try
        {
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options =>
            {
                options.DelayAfter = 6;
                options.FakeDelay = true;
                options.Skip = _ => true;
            });
            
            // Start the test server.
            var host = await builder.StartAsync(TestContext.Current.CancellationToken);
            var client = host.GetTestClient();
            
            // Set a current count for the user.
            var cache = host.GetTestServer().Services.GetRequiredService<CacheHelper>();
            await cache.Set("127.0.0.1", 5);

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            var response = await client.SendAsync(message, TestContext.Current.CancellationToken);

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
        await CacheSemaphore.Semaphore.WaitAsync(TestContext.Current.CancellationToken);
        
        try
        {
            // Start the test server.
            var builder = UnitTestHelperMethods.CreateWebHostBuilder();
            var host = await builder.StartAsync(TestContext.Current.CancellationToken);
            var client = host.GetTestClient();

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);
            
            // Send the request.
            _ = await client.SendAsync(message, TestContext.Current.CancellationToken);
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }

    [Fact]
    public async Task InvokeAsync_WorksWithSlowDownDisabled()
    {
        await CacheSemaphore.Semaphore.WaitAsync(TestContext.Current.CancellationToken);

        try
        {
            // Start the test server.
            var builder = UnitTestHelperMethods.CreateWebHostBuilder(options => { options.SlowDownEnabled = false; });
            var host = await builder.StartAsync(TestContext.Current.CancellationToken);
            var client = host.GetTestClient();

            // Create an HttpRequestMessage to send to the test server.
            var context = UnitTestHelperMethods.CreateXForwardedForHttpContext();
            var message = UnitTestHelperMethods.ConvertToHttpRequestMessage(context.Request);

            // Send the request.
            _ = await client.SendAsync(message, TestContext.Current.CancellationToken);
        }
        finally
        {
            CacheSemaphore.Semaphore.Release();
        }
    }
}
