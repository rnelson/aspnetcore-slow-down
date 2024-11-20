using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
}
