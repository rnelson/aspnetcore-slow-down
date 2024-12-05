using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nearform.AspNetCore.SlowDown;

namespace SlowDown.Tests;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
internal static class UnitTestHelperMethods
{
    public static HttpRequestMessage ConvertToHttpRequestMessage(HttpRequest request)
    {
        var message = new HttpRequestMessage
        {
            Content = new StreamContent(request.Body),
            Method = string.IsNullOrEmpty(request.Method) ? HttpMethod.Get : new(request.Method)
        };
        
        foreach (var header in request.Headers)
            message.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        
        return message;
    }
    
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

    public static HostBuilder CreateWebHostBuilder(Action<SlowDownOptions>? configAction = null) =>
        (new HostBuilder()
            .ConfigureWebHost(builder =>
            {
                builder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
#pragma warning disable EXTEXP0018
                        services.AddHybridCache();
                        services.AddSlowDown(configAction);
#pragma warning restore EXTEXP0018
                    })
                    .Configure(app =>
                    {
                        app.UseSlowDown();
                    });
            }) as HostBuilder)!;

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