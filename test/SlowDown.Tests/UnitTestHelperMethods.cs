using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nearform.AspNetCore.SlowDown;

namespace SlowDown.Tests;

internal static class UnitTestHelperMethods
{
    public static DefaultHttpContext CreateHttpContext() => new();
    
    public static SlowDownMiddleware CreateSlowDownMiddleware() =>
        new(_ => Task.CompletedTask, NullLogger<SlowDownMiddleware>.Instance);
    
    public static CancellationToken GetCancellationToken()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        return cancellationTokenSource.Token;
    }
    
    public static CancellationTokenSource GetCancelledCancellationTokenSource()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        
        return cancellationTokenSource;
    }
    
    public static CancellationToken GetFutureCancellationToken(int millisecondsTimeout)
    {
        var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromMilliseconds(millisecondsTimeout));

        return source.Token;
    }

    public static CancellationToken GetFutureCancellationToken(SlowDownOptions options) =>
        GetFutureCancellationToken(options.CacheTimeout);
    
    public static Tuple<HybridCache, HttpRequest> Setup()
    {
        var request = CreateXForwardedForHttpRequest();
        var cache = CreateCache();
        
        SlowDownOptions.CurrentOptions.Cache = cache;
        
        return new Tuple<HybridCache, HttpRequest>(cache, request);
    }
    
    private static HttpRequest CreateXForwardedForHttpRequest()
    {
        const string expected = "4.2.2.4";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = expected;
        
        return context.Request;
    }
    
    public static HybridCache CreateCache()
    {
        var builder = WebApplication.CreateBuilder();

#pragma warning disable EXTEXP0018
        builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018
        
        var cache = builder
            .Services
            .BuildServiceProvider()
            .GetService<HybridCache>();
        return cache!;
    }
}