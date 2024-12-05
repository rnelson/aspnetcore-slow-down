using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nearform.AspNetCore.SlowDown;

namespace SlowDown.Tests;

public class SlowDownMiddlewareExtensionsTests(SlowDownOptions options)
    : IClassFixture<SlowDownOptions>
{
    private static readonly SemaphoreSlim Semaphore = new(1,1);
    private readonly SlowDownOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    
    [Fact]
    public async Task MiddlewareExtensions_Work()
    {
        await Semaphore.WaitAsync();

        try
        {
            var builder = WebApplication.CreateBuilder();
            var services = builder.Services;
            services.AddSlowDown();

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

            Assert.False(_options.SlowDownEnabled);
            Assert.Equal(16, _options.Delay);
            Assert.Equal(32, _options.DelayAfter);
            Assert.Equal(64, _options.MaxDelay);
            Assert.Equal(128, _options.TimeWindow);
            Assert.False(_options.AddHeaders);
            Assert.True(_options.SkipFailedRequests);
            Assert.True(_options.SkipSuccessfulRequests);
            Assert.Equal(256, _options.CacheTimeout);
        }
        finally
        {
            Semaphore.Release();
        }
    }
}
