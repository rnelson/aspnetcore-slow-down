using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nearform.AspNetCore.SlowDown;
using Xunit.DependencyInjection;

namespace SlowDown.Tests;

[Startup(typeof(Startup))]
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
            
            var opt = services.BuildServiceProvider().GetRequiredService<SlowDownOptions>();

            Assert.False(opt.SlowDownEnabled);
            Assert.Equal(16, opt.Delay);
            Assert.Equal(32, opt.DelayAfter);
            Assert.Equal(64, opt.MaxDelay);
            Assert.Equal(128, opt.TimeWindow);
            Assert.False(opt.AddHeaders);
            Assert.True(opt.SkipFailedRequests);
            Assert.True(opt.SkipSuccessfulRequests);
            Assert.Equal(256, opt.CacheTimeout);
        }
        finally
        {
            Semaphore.Release();
        }
    }
}
