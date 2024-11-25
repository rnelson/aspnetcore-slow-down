using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nearform.AspNetCore.SlowDown;

namespace SlowDown.Tests;

public class SlowDownMiddlewareExtensionsTests
{
    private static readonly SemaphoreSlim Semaphore = new(1,1);
    
    [Fact]
    public async Task MiddlewareExtensions_Work()
    {
        await Semaphore.WaitAsync();

        try
        {
            var builder = WebApplication.CreateBuilder();
            var services = builder.Services;

            services.AddSlowDown(config =>
            {
                config.Cache = UnitTestHelperMethods.CreateCache();
            });

            await using var app = builder.Build();

            app.UseRouting();
            app.UseSlowDown();
        }
        finally
        {
            Semaphore.Release();
        }
    }

    [Fact]
    public async Task MiddlewareExtensions_Configuration_Works()
    {
        await Semaphore.WaitAsync();

        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile("test.json");

        try
        {
            var builder = WebApplication.CreateBuilder();
            
            var services = builder.Services;
            services.AddSingleton<IConfiguration>(configBuilder.Build());
            services.AddSlowDown();

            await using var app = builder.Build();
            app.UseRouting();
            app.UseSlowDown();

            Assert.False(SlowDownOptions.CurrentOptions.SlowDownEnabled);
            Assert.Equal(16, SlowDownOptions.CurrentOptions.Delay);
            Assert.Equal(32, SlowDownOptions.CurrentOptions.DelayAfter);
            Assert.Equal(64, SlowDownOptions.CurrentOptions.MaxDelay);
            Assert.Equal(128, SlowDownOptions.CurrentOptions.TimeWindow);
            Assert.False(SlowDownOptions.CurrentOptions.AddHeaders);
            Assert.True(SlowDownOptions.CurrentOptions.SkipFailedRequests);
            Assert.True(SlowDownOptions.CurrentOptions.SkipSuccessfulRequests);
            Assert.Equal(256, SlowDownOptions.CurrentOptions.CacheTimeout);
        
            /*
    {
      "SlowDown": {
        "SlowDownEnabled": false,
        "Delay": 16,
        "DelayAfter": 32,
        "MaxDelay": 64,
        "TimeWindow": 128,
        "AddHeaders": false,
        "SkipFailedRequests": true,
        "SkipSuccessfulRequests": true,
        "CacheTimeout": 256
      }
    }
             */
        }
        finally
        {
            Semaphore.Release();
        }
    }
}
