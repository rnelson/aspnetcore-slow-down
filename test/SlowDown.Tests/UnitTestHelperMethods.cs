using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Nearform.AspNetCore.SlowDown;

namespace SlowDown.Tests;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
internal static class UnitTestHelperMethods
{
    public static CancellationToken CreateCancellationToken()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        return cancellationTokenSource.Token;
    }
    
    public static CancellationTokenSource CreateCancelledCancellationTokenSource()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        
        return cancellationTokenSource;
    }
    
    public static CancellationToken CreateFutureCancellationToken(int millisecondsTimeout)
    {
        var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromMilliseconds(millisecondsTimeout));

        return source.Token;
    }

    public static CancellationToken CreateFutureCancellationToken(SlowDownOptions options) =>
        CreateFutureCancellationToken(options.CacheTimeout);
    
    public static DefaultHttpContext CreateHttpContext() => new();

    public static Func<HttpRequest, CancellationToken, Task<string>> CreateKeyGenerator(string response) =>
        (_, _) => Task.FromResult(response);

    // public static SlowDownMiddleware CreateSlowDownMiddleware()
    // {
    //     var services = Services.BuildServiceProvider();
    //     var options = services.GetRequiredService<SlowDownOptions>();
    //     var cacheHelper = services.GetRequiredService<CacheHelper>();
    //     
    //     return new(_ => Task.CompletedTask,
    //         NullLogger<SlowDownMiddleware>.Instance,
    //         options,
    //         cacheHelper);
    // }

    public static HttpContext CreateXForwardedForHttpContext(string ip = "127.0.0.1")
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = ip;
        
        return context;
    }
    
    public static HttpRequest CreateXForwardedForHttpRequest()
    {
        const string expected = "4.2.2.4";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = expected;
        
        return context.Request;
    }
}